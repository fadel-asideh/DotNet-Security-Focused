using DotNetSecurityFocused.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DotNetSecurityFocused.Tests.Tests;

public class SecurityHeadersTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public SecurityHeadersTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Response_IncludesSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task HttpsResponse_IncludesHstsHeader()
    {
        // TestServer only sets IsHttps (and therefore the HSTS header) based on the
        // request URI's scheme, so the client base address has to actually say https.
        // localhost is on HstsMiddleware's default ExcludedHosts list (by design, so local dev
        // over https://localhost never gets HSTS-pinned), so use a non-localhost host here.
        var httpsClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://example.com")
        });

        var response = await httpsClient.GetAsync("/health");

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }
}
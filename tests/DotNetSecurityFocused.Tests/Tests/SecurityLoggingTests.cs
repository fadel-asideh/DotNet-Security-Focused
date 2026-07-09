using System.Net;
using System.Net.Http.Json;
using DotNetSecurityFocused.Tests.Fixtures;
using DotNetSecurityFocused.Tests.Helpers;

namespace DotNetSecurityFocused.Tests.Tests;

public class SecurityLoggingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public SecurityLoggingTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FailedLogin_LogsSecurityEvent_WithoutPassword()
    {
        const string password = "WrongPassword!";
        await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "logging_probe@test.com",
            password
        });

        Assert.Contains(_factory.LogProvider.Entries,
            e => e.Contains("LoginFailed") && e.Contains("logging_probe@test.com"));
        Assert.DoesNotContain(_factory.LogProvider.Entries, e => e.Contains(password));
    }

    [Fact]
    public async Task RoleAssigned_OnRegister_LogsSecurityEvent()
    {
        await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "logging_role_probe@test.com",
            password = "Test@123!",
            confirmPassword = "Test@123!",
            roles = new[] { "User" }
        });

        Assert.Contains(_factory.LogProvider.Entries,
            e => e.Contains("RoleAssigned") && e.Contains("User"));
    }

    [Fact]
    public async Task RateLimitRejected_LogsSecurityEvent()
    {
        HttpResponseMessage? last = null;
        for (int i = 0; i < 25; i++)
        {
            last = await _client.PostAsJsonAsync("/auth/login", new
            {
                email = "logging_ratelimit_probe@test.com",
                password = "WrongPassword!"
            });
            if (last.StatusCode == HttpStatusCode.TooManyRequests) break;
        }

        Assert.Contains(_factory.LogProvider.Entries, e => e.Contains("RateLimitRejected"));
    }

    [Fact]
    public async Task AuthorizationFailure_LogsSecurityEvent()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "logging_authz_probe@test.com", "User");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin");

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(_factory.LogProvider.Entries,
            e => e.Contains("AuthorizationFailure") && e.Contains("/secrets/admin"));
    }
}

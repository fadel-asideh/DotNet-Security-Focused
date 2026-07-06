using System.Net;
using System.Net.Http.Json;
using DotNetSecurityFocused.Tests.Fixtures;

namespace DotNetSecurityFocused.Tests.Tests;

public class RateLimitTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public RateLimitTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task login_ExceedsRateLimit_Return429()
    {
       HttpResponseMessage? lastResponse = null;
       for(int i = 0; i < 25; i++)
       {
            lastResponse = await _client.PostAsJsonAsync("/auth/login", new
            {
                email = "ratelimit_probe@test.com",
                password = "WrongPassword!"
            });

            if (lastResponse.StatusCode == HttpStatusCode.TooManyRequests)
                break;
       }
       Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }
}
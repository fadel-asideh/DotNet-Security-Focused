using System.Net;
using System.Net.Http.Json;
using DotNetSecurityFocused.Models.DTOs;
using DotNetSecurityFocused.Tests.Fixtures;


namespace DotNetSecurityFocused.Tests.Tests;

public class RefreshTokenTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public RefreshTokenTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    private async Task<LoginResponse> RegisterAndLoginAsync(string email)
    {
        await _client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test@123!",
            ConfirmPassword = "Test@123!",
            Roles = new[] { "User" }
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Test@123!"
        });

        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewTokens()
    {
        var login = await RegisterAndLoginAsync("refresh_valid@test.com");

        var response = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = login.RefreshToken });

        response.EnsureSuccessStatusCode();
        var refreshed = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(login.RefreshToken, refreshed!.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithRotatedAwayToken_ReturnsUnauthorized()
    {
        var login = await RegisterAndLoginAsync("refresh_reuse@test.com");

        await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = login.RefreshToken });
        var reuseResponse = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = login.RefreshToken });

        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_ThenRefresh_ReturnsUnauthorized()
    {
        var login = await RegisterAndLoginAsync("refresh_logout@test.com");

        var logoutResponse = await _client.PostAsJsonAsync("/auth/logout", new { refreshToken = login.RefreshToken });
        logoutResponse.EnsureSuccessStatusCode();

        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", new { refreshToken = login.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DotNetSecurityFocused.Models;
using DotNetSecurityFocused.Tests.Fixtures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DotNetSecurityFocused.Tests.Helpers;

internal class TokenResponse
{
    public string Token { get; set; } = string.Empty;
}

public static class AuthHelper
{
    public static async Task<string> GetTokenAsync(
        HttpClient client,
        string email,
        params string[] roles)
    {
        await client.PostAsJsonAsync("/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test@123!",
            Roles = roles
        });

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Test@123!"
        });

        var result = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        return result!.Token;
    }

    public static HttpClient CreateClientWithToken(ApiFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static string GenerateExpiredToken(ApiFactory factory)
    {
        var config = factory.Services.GetRequiredService<IConfiguration>();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: [],
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
using System.Net;
using System.Net.Http.Json;
using DotNetSecurityFocused.Tests.Fixtures;
using DotNetSecurityFocused.Tests.Helpers;

namespace DotNetSecurityFocused.Tests.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ===========================
    // Register
    // ===========================

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "register_valid@test.com",
            password = "Test@123!",
            roles = new[] { "Admin" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "register_invalid_role@test.com",
            password = "Test@123!",
            roles = new[] { "SuperAdmin" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var payload = new
        {
            email = "duplicate@test.com",
            password = "Test@123!",
            roles = new[] { "User" }
        };

        await _client.PostAsJsonAsync("/auth/register", payload);
        var response = await _client.PostAsJsonAsync("/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ===========================
    // Login
    // ===========================

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "login_valid@test.com",
            password = "Test@123!",
            roles = new[] { "User" }
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "login_valid@test.com",
            password = "Test@123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", body);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "login_wrong@test.com",
            password = "Test@123!",
            roles = new[] { "User" }
        });

        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "login_wrong@test.com",
            password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            email = "ghost@test.com",
            password = "Test@123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

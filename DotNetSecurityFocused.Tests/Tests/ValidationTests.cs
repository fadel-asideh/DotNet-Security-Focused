using System.Net;
using System.Net.Http.Json;
using DotNetSecurityFocused.Tests.Fixtures;

namespace DotNetSecurityFocused.Tests.Tests;

public class ValidationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ValidationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_MissingEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            password = "Test@123!",
            confirmPassword = "Test@123!",
            roles = new[] { "User" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordTooShort_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "short_pw@test.com",
            password = "abc",
            confirmPassword = "abc",
            roles = new[] { "User" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    
    [Fact]
    public async Task Register_ConfirmPasswordMismatch_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "mismatch@test.com",
            password = "Test@123!",
            confirmPassword = "Different@123!",
            roles = new[] { "User" }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_EmptyRolesArray_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "no_roles@test.com",
            password = "Test@123!",
            confirmPassword = "Test@123!",
            roles = Array.Empty<string>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/auth/register", new
        {
            email = "validation_valid@test.com",
            password = "Test@123!",
            confirmPassword = "Test@123!",
            roles = new[] { "User" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
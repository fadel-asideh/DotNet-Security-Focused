using System.Net;
using DotNetSecurityFocused.Tests.Fixtures;
using DotNetSecurityFocused.Tests.Helpers;

namespace DotNetSecurityFocused.Tests.Tests;

public class SecretsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public SecretsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ===========================
    // No token
    // ===========================

    [Fact]
    public async Task AccessAnySecretEndpoint_WithNoToken_Returns401()
    {
        var response = await _client.GetAsync("/secrets/admin");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===========================
    // Expired token
    // ===========================

    [Fact]
    public async Task AccessAnySecretEndpoint_WithExpiredToken_Returns401()
    {
        var expiredToken = AuthHelper.GenerateExpiredToken(_factory);
        var client = AuthHelper.CreateClientWithToken(_factory, expiredToken);

        var response = await client.GetAsync("/secrets/admin");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ===========================
    // Admin endpoint
    // ===========================

    [Fact]
    public async Task AccessAdminEndpoint_WithAdminToken_Returns200()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "admin_200@test.com", "Admin");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminEndpoint_WithUserToken_Returns403()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "user_admin_403@test.com", "User");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminEndpoint_WithManagerToken_Returns403()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "manager_admin_403@test.com", "Manager");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ===========================
    // User endpoint
    // ===========================

    [Fact]
    public async Task AccessUserEndpoint_WithUserToken_Returns200()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "user_200@test.com", "User");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/user");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessUserEndpoint_WithAdminToken_Returns403()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "admin_user_403@test.com", "Admin");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/user");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ===========================
    // Manager endpoint
    // ===========================

    [Fact]
    public async Task AccessManagerEndpoint_WithManagerToken_Returns200()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "manager_200@test.com", "Manager");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/manager");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessManagerEndpoint_WithUserToken_Returns403()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "user_manager_403@test.com", "User");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/manager");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ===========================
    // Admin OR Manager endpoint
    // ===========================

    [Fact]
    public async Task AccessAdminOrManagerEndpoint_WithAdminToken_Returns200()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "admin_or_manager_1@test.com", "Admin");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin-or-manager");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminOrManagerEndpoint_WithManagerToken_Returns200()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "admin_or_manager_2@test.com", "Manager");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin-or-manager");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminOrManagerEndpoint_WithUserToken_Returns403()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "user_or_403@test.com", "User");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin-or-manager");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ===========================
    // Admin AND Manager endpoint
    // ===========================

    [Fact]
    public async Task AccessAdminAndManagerEndpoint_WithBothRoles_Returns200()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "adminmanager_200@test.com", "Admin", "Manager");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin-and-manager");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminAndManagerEndpoint_WithAdminOnly_Returns403()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "admin_and_403@test.com", "Admin");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin-and-manager");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AccessAdminAndManagerEndpoint_WithManagerOnly_Returns403()
    {
        var token = await AuthHelper.GetTokenAsync(_client, "manager_and_403@test.com", "Manager");
        var client = AuthHelper.CreateClientWithToken(_factory, token);

        var response = await client.GetAsync("/secrets/admin-and-manager");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
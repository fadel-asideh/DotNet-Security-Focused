using System.Net;
using System.Net.Http.Json;
using DotNetSecurityFocused.Models.Entities;
using DotNetSecurityFocused.Services;
using DotNetSecurityFocused.Tests.Fixtures;
using DotNetSecurityFocused.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;


public class OrderAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OrderAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(string userId, HttpClient client)> CreateAuthenticatedUserAsync(string email)
    {
        var token = await AuthHelper.GetTokenAsync(_factory.CreateClient(), email, "User");
        var client = AuthHelper.CreateClientWithToken(_factory, token);
        return (email, client); // email is unique per test and doubles as a stable identifier for assertions
    }

    // Test 1 — prove the vulnerable service method leaks another user's order
    [Fact]
    public async Task GetOrderByIdVulnerable_ReturnsOrder_RegardlessOfOwner()
    {
        var (_, ownerClient) = await CreateAuthenticatedUserAsync("order_owner1@test.com");
        var createResponse = await ownerClient.PostAsJsonAsync("/orders", new CreateOrderRequest
        {
            ProductName = "Widget",
            Quantity = 1,
            TotalPrice = 9.99m
        });
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<OrderService>();

        var result = await service.GetOrderByIdVulnerableAsync(created!.Id);

        Assert.NotNull(result); // no ownership check at all - this is the bug this task fixes
    }

     // Test 2 — the real endpoint refuses cross-user access
    [Fact]
    public async Task GetOrderById_AnotherUsersOrder_ReturnsNotFound()
    {
        var (_, ownerClient) = await CreateAuthenticatedUserAsync("order_owner2@test.com");
        var createResponse = await ownerClient.PostAsJsonAsync("/orders", new CreateOrderRequest
        {
            ProductName = "Gadget",
            Quantity = 2,
            TotalPrice = 19.99m
        });
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();

        var (_, attackerClient) = await CreateAuthenticatedUserAsync("order_attacker2@test.com");
        var response = await attackerClient.GetAsync($"/orders/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Test 3 — the legitimate owner can still fetch their own order
    [Fact]
    public async Task GetOrderById_OwnOrder_ReturnsOrder()
    {
        var (_, ownerClient) = await CreateAuthenticatedUserAsync("order_owner3@test.com");
        var createResponse = await ownerClient.PostAsJsonAsync("/orders", new CreateOrderRequest
        {
            ProductName = "Doohickey",
            Quantity = 3,
            TotalPrice = 29.99m
        });
        var created = await createResponse.Content.ReadFromJsonAsync<Order>();

        var response = await ownerClient.GetAsync($"/orders/{created!.Id}");

        response.EnsureSuccessStatusCode();
        var fetched = await response.Content.ReadFromJsonAsync<Order>();
        Assert.Equal(created.Id, fetched!.Id);
    }
}
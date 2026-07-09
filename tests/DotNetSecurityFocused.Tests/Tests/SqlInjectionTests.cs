using System.Net.Http.Json;
using DotNetSecurityFocused.Models.Entities;
using DotNetSecurityFocused.Services;
using DotNetSecurityFocused.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetSecurityFocused.Tests.Tests;

public class SqlInjectionTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public SqlInjectionTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    //Test 1 — prove the vulnerable method leaks everything
    [Fact]
    public async Task SearchByNameVulnerable_WithInjectionPayload_ReturnsAllProducts()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ProductSearchService>();

        var results = await service.SearchByNameVulnerableAsync("x' OR '1'='1' -- ");

        Assert.Contains(results, p => p.Name == "Admin Override Key");
    }
    
    //Test 2 — prove the safe method treats it as literal text
    [Fact]
    public async Task SearchByNameSafe_WithInjectionPayload_ReturnsNoResults()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ProductSearchService>();

        var results = await service.SearchByNameSafeAsync("x' OR '1'='1' -- ");

        Assert.Empty(results);
    }

    //Test 3 — happy path regression check
    [Fact]
    public async Task SearchByNameSafe_WithLegitimateTerm_ReturnsMatch()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ProductSearchService>();

        var results = await service.SearchByNameSafeAsync("Widget");

        Assert.Contains(results, p => p.Name == "Widget");
    }

    //Test 4 — end-to-end via the actual HTTP endpoint
    [Fact]
    public async Task SearchEndpoint_WithInjectionPayload_ReturnsEmptyArray()
    {
        var payload = Uri.EscapeDataString("x' OR '1'='1' -- ");
        var response = await _client.GetAsync($"/products/search?name={payload}");

        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();

        Assert.NotNull(products);
        Assert.Empty(products);
    }

}
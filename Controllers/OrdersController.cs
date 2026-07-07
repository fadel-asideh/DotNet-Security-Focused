using DotNetSecurityFocused.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DotNetSecurityFocused.Controllers;

[Controller]
[Route("[Controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var userId = GetCurrentUserId();
        var order = await _orderService.CreateOrderAsync(userId, request.ProductName, request.Quantity, request.TotalPrice);
        return Ok(order);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");

        var order = await _orderService.GetOrderByIdSafeAsync(id, userId, isAdmin);
        if (order == null) return NotFound();

        return Ok(order);
    }

    private string GetCurrentUserId()
    {
        // "sub" isn't guaranteed to be remapped to ClaimTypes.NameIdentifier depending on
        // JWT handler defaults - check both so this doesn't silently break across .NET versions.
        return User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User id claim missing from token.");
    }
}
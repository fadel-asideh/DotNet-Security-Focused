using DotNetSecurityFocused.Data;
using DotNetSecurityFocused.Models.Entities;

namespace DotNetSecurityFocused.Services;

public class OrderService
{
    private readonly AppDBContext _context;

    public OrderService(AppDBContext context)
    {
        _context = context;
    }

    public async Task<Order> CreateOrderAsync(string userId, string productName, int quantity, decimal totalPrice)
    {
        var order = new Order
        {
            UserId = userId,
            ProductName = productName,
            Quantity = quantity,
            TotalPrice = totalPrice
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    // Intentionally vulnerable reference - returns any order by ID with no ownership check.
    // Never wired to a controller route; kept only to document the IDOR this task guards against.
    public async Task<Order?> GetOrderByIdVulnerableAsync(int id)
    {
        return await _context.Orders.FindAsync(id);
    }

    public async Task<Order?> GetOrderByIdSafeAsync(int id, string requestingUserId, bool isAdmin)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return null;

        // Not the owner and not an Admin - treat as not found so we don't leak whether the ID exists.
        if (!isAdmin && order.UserId != requestingUserId) return null;

        return order;
    }
}
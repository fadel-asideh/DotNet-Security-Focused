using DotNetSecurityFocused.Data;
using DotNetSecurityFocused.Models;
using Microsoft.EntityFrameworkCore;


namespace DotNetSecurityFocused.Services;

public class ProductSearchService
{
    private readonly AppDBContext _context;

    public ProductSearchService(AppDBContext appDBContext)
    {
        _context = appDBContext;        
    }

    // VULLNERABLE - refrence example only. never called from a controller.
    public async Task<List<Product>> SearchByNameVulnerableAsync(string name)
    {
        return await _context.Products
            .FromSqlRaw($"SELECT * FROM Products WHERE Name LIKE '%{name}%'")
            .ToListAsync();
    }

    // SAFE - preferred approach, plain LINQ
    public async Task<List<Product>> SearchByNameSafeAsync(string name)
    {
        return await _context.Products.Where(p => p.Name.Contains(name)).ToListAsync();
    }

    // SAFE - alternative when raw sql is unavoidable
    public async Task<List<Product>> SearchByNameSafeRawAsync(string name)
    {
        return await _context.Products
            .FromSqlInterpolated($"SELECT * FROM Products WHERE Name LIKE '%' || {name} || '%'")
            .ToListAsync();
    }

}
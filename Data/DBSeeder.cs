using Microsoft.AspNetCore.Identity;
using DotNetSecurityFocused.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetSecurityFocused.Data;

public static class DBSeeder
{

    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        await SeedRolesAsync(serviceProvider);
        await SeedProductsAsync(serviceProvider);
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = new[] { "Admin", "Manager" ,"User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedProductsAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDBContext>();
        
        if(!await context.Products.AnyAsync())
        {
            context.Products.AddRange(
                new Product { Name = "Widget", Description = "A basic widget", Price = 9.99m },
                new Product { Name = "Gadget", Description = "A fancy gadget", Price = 19.99m },
                new Product { Name = "Admin Override Key", Description = "Should never be surfaced by normal search", Price = 999.99m }
            );
            
            await context.SaveChangesAsync();
        }
    }
}
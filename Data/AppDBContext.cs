using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DotNetSecurityFocused.Models.Entities;

namespace DotNetSecurityFocused.Data;
public class AppDBContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
    {
        
    }
}
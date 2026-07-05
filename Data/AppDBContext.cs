using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DotNetSecurityFocused.Models;

namespace DotNetSecurityFocused.Data;
public class AppDBContext : IdentityDbContext<ApplicationUser>
{
    public AppDBContext(DbContextOptions<AppDBContext> options) : base(options)
    {
    }
}
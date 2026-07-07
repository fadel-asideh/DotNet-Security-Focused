using DotNetSecurityFocused.Data;
using DotNetSecurityFocused.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using DotNetSecurityFocused.Services;
using FluentValidation;
using DotNetSecurityFocused.Validators;
using DotNetSecurityFocused.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<AppDBContext>(options => options.UseSqlite("Data Source=app.db"));
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddAppRateLimiting();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<ProductSearchService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DBSeeder.SeedDataAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
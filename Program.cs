using DotNetSecurityFocused.Data;
using DotNetSecurityFocused.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using DotNetSecurityFocused.Services;
using FluentValidation;
using DotNetSecurityFocused.Validators;
using DotNetSecurityFocused.Extensions;
using DotNetSecurityFocused.Authorization;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<AppDBContext>(options => options.UseSqlite("Data Source=app.db"));
builder.Services.AddAppAuthentication(builder.Configuration);
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, LoggingAuthorizationMiddlewareResultHandler>();
builder.Services.AddAppRateLimiting();
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<ProductSearchService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddSingleton<ISecurityEventLogger, SecurityEventLogger>();

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
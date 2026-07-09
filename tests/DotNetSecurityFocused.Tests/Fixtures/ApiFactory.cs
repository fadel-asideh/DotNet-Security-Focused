using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetSecurityFocused.Data;

namespace DotNetSecurityFocused.Tests.Fixtures;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public ListLoggerProvider LogProvider { get; } = new();
    
    public ApiFactory()
    {
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDBContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new AppDBContext(options);
        db.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real AppDbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDBContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Replace with an isolated in-memory SQLite database
            services.AddDbContext<AppDBContext>(options =>
                options.UseSqlite(_connection));
        });

         builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "this-is-a-super-secret-key-for-dev-only-make-it-long",
                ["Jwt:Issuer"] = "DotNetSecurityFocused",
                ["Jwt:Audience"] = "DotNetSecurityFocusedUsers"
            });
        });

        builder.ConfigureLogging(logging =>
        {
            logging.AddProvider(LogProvider);
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
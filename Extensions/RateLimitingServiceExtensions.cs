using System.Threading.RateLimiting;
using DotNetSecurityFocused.Services;

namespace DotNetSecurityFocused.Extensions;

public static class RateLimitingServiceExtensions
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("ip-sliding", HttpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _=> new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        SegmentsPerWindow = 4,
                        Window = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
            options.OnRejected = async (context, token) =>
            {
                var securityEventLogger = context.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>();
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                securityEventLogger.LogRateLimitRejected(ipAddress, context.HttpContext.Request.Path);
                
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
            };
        });

        return services;
    }
}
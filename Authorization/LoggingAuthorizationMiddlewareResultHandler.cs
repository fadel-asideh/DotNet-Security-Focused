using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotNetSecurityFocused.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace DotNetSecurityFocused.Authorization;

public class LoggingAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly ISecurityEventLogger _securityEventLogger;

    public LoggingAuthorizationMiddlewareResultHandler(ISecurityEventLogger securityEventLogger)
    {
        _securityEventLogger = securityEventLogger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var userId = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            _securityEventLogger.LogAuthorizationFailure(userId, context.Request.Path);
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
namespace DotNetSecurityFocused.Services;

public interface ISecurityEventLogger
{
    void LogLoginFailed(string email, string ipAddress);
    void LogRateLimitRejected(string ipAddress, string path);
    void LogRoleAssigned(string userId, string role);
    void LogAuthorizationFailure(string userId, string path);
}

public class SecurityEventLogger : ISecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    public SecurityEventLogger(ILogger<SecurityEventLogger> logger)
    {
        _logger = logger;
    }

    public void LogLoginFailed(string email, string ipAddress) =>
        _logger.LogWarning(
            "SecurityEvent: {EventType} Email={Email} Ip={Ip} Timestamp={Timestamp}",
            "LoginFailed", email, ipAddress, DateTime.UtcNow);

    public void LogRateLimitRejected(string ipAddress, string path) =>
        _logger.LogWarning(
            "SecurityEvent: {EventType} Ip={Ip} Path={Path} Timestamp={Timestamp}",
            "RateLimitRejected", ipAddress, path, DateTime.UtcNow);

    public void LogRoleAssigned(string userId, string role) =>
        _logger.LogInformation(
            "SecurityEvent: {EventType} UserId={UserId} Role={Role} Timestamp={Timestamp}",
            "RoleAssigned", userId, role, DateTime.UtcNow);
    
    public void LogAuthorizationFailure(string userId, string path) =>
    _logger.LogWarning(
        "SecurityEvent: {EventType} UserId={UserId} Path={Path} Timestamp={Timestamp}",
        "AuthorizationFailure", userId, path, DateTime.UtcNow);
}
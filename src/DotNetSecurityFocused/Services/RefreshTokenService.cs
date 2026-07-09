using System.Security.Cryptography;
using System.Text;
using DotNetSecurityFocused.Data;
using DotNetSecurityFocused.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotNetSecurityFocused.Services;

public class RefreshTokenService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);
    private readonly AppDBContext _context;

    public RefreshTokenService(AppDBContext context)
    {
        _context = context;
    }

    public async Task<string> IssueAsync(string userId)
    {
        (string rowToken, RefreshToken refreshToken) = AddNewToken(userId);
        
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return rowToken;
    }

    // Validates the raw token and, if valid, revokes it and issues a replacement (rotation).
    // Returns null if the token is missing, expired, or already revoked/rotated-away.
    public async Task<(string UserId, string NewRefreshToken)?> RotateAsync(string rawToken)
    {
        var existing = await FindActiveAsync(rawToken);
        if(existing == null) return null;
        
        existing.RevokedAt = DateTime.UtcNow;
        (string newRawToken, RefreshToken refreshToken) = AddNewToken(existing.UserId);
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return (existing.UserId, newRawToken);
    }

    public async Task<bool> RevokeAsync(string rawToken)
    {
        var existing = await FindActiveAsync(rawToken);
        if (existing == null) return false;

        existing.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<RefreshToken?> FindActiveAsync(string rawToken)
    {
        var tokenHash = Hash(rawToken);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (token == null || token.RevokedAt != null || token.ExpiresAt < DateTime.UtcNow)
            return null;
        
        return token;
    }

    private ( string RawToken, RefreshToken refreshToken) AddNewToken(string userId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.Add(RefreshTokenLifetime)
        };
        return (rawToken, refreshToken);
    }

    private static string Hash(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

}
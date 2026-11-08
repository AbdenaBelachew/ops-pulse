using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace OpsPulse.Infrastructure.Identity;

/// <summary>
/// Issues short-lived signed access tokens (JWT) and opaque refresh tokens.
/// Refresh tokens are stored hashed; rotation is handled by the identity service.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _jwt = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        Guid userId,
        string email,
        string? fullName,
        Guid? organizationId,
        string? role)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (organizationId is { } organizationIdValue)
        {
            claims.Add(new Claim("org", organizationIdValue.ToString()));
        }

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim("role", role));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public (string Token, DateTime ExpiresAtUtc) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return (Base64Url.EncodeToString(bytes), DateTime.UtcNow.AddDays(_jwt.RefreshTokenLifetimeDays));
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

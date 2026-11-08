using OpsPulse.Domain.Common;

namespace OpsPulse.Domain.Identity;

/// <summary>
/// A refresh token allowing a client to obtain new access tokens. Only the
/// SHA-256 hash of the token is stored; rotation revokes the previous token
/// and links it to its replacement for reuse detection.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow <= ExpiresAtUtc;
}

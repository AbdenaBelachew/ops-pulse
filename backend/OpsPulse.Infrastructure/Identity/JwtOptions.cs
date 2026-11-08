namespace OpsPulse.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "OpsPulse.API";

    public string Audience { get; set; } = "OpsPulse.Web";

    /// <summary>HS-256 signing key (at least 32 bytes). Never commit production keys.</summary>
    public string Key { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 7;
}

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Security;

/// <summary>
/// Authenticates monitoring agents via the X-Api-Key header. Keys are stored
/// hashed; successful authentication yields an org-scoped principal.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue) ||
            string.IsNullOrWhiteSpace(headerValue.ToString()))
        {
            return AuthenticateResult.NoResult();
        }

        var keyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(headerValue.ToString())));

        var db = Context.RequestServices.GetRequiredService<OpsPulseDbContext>();
        var apiKey = await db.ApiKeys
            .Include(k => k.Organizations)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

        if (apiKey is null)
        {
            return AuthenticateResult.Fail("Invalid or revoked API key.");
        }

        // Throttle last-used writes to one per minute per key.
        if (apiKey.LastUsedAtUtc is null || DateTime.UtcNow - apiKey.LastUsedAtUtc > TimeSpan.FromMinutes(1))
        {
            apiKey.LastUsedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new("org", apiKey.OrganizationId.ToString()),
            new("name", $"api-key:{apiKey.Name}"),
            new("role", "Agent")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}

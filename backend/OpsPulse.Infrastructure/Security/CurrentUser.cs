using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OpsPulse.Application.Common;
using OpsPulse.Domain.Common;

namespace OpsPulse.Infrastructure.Security;

/// <summary>
/// Resolves the authenticated principal from validated JWT claims
/// (claim types configured in the JWT bearer options: sub, email, name, org, role).
/// Works for both user tokens and agent API-key principals (org claim).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? throw new UnauthorizedException("No authenticated context.");

    public Guid UserId =>
        Guid.TryParse(Principal.FindFirstValue("sub"), out var id)
            ? id
            : throw new UnauthorizedException("Invalid token subject.");

    public string Email => Principal.FindFirstValue("email") ?? string.Empty;

    public Guid? OrganizationId =>
        Guid.TryParse(Principal.FindFirstValue("org"), out var organizationId)
            ? organizationId
            : null;

    public string Role => Principal.FindFirstValue("role") ?? string.Empty;

    public bool IsSuperAdmin => Principal.IsInRole(RoleNames.SuperAdmin);
}

using Microsoft.AspNetCore.Identity;

namespace OpsPulse.Infrastructure.Identity;

/// <summary>
/// Application user extending ASP.NET Core Identity with OpsPulse-specific fields.
/// Lives in Infrastructure to keep the Domain layer independent of Identity.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public Guid? OrganizationId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

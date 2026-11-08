using OpsPulse.Domain.Common;
using OpsPulse.Domain.Services;

namespace OpsPulse.Domain.Organizations;

/// <summary>
/// A tenant that owns every monitored resource in the platform. All
/// organization-owned records are isolated through an <c>OrganizationId</c> foreign key.
/// </summary>
public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MonitoredService> MonitoredServices { get; set; } = [];
}

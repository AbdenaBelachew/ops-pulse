using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Security;

/// <summary>
/// An API key used by monitoring agents to authenticate ingest requests.
/// Only the SHA-256 hash is stored.
/// </summary>
public class ApiKey : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;

    /// <summary>Prefix displayed in the UI for identification (first 8 chars of the raw key).</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? LastUsedAtUtc { get; set; }
}

using OpsPulse.Domain.Common;

namespace OpsPulse.Domain.Audit;

/// <summary>
/// Tamper-evident trail of security-relevant actions: logins, configuration
/// changes, user/role changes and incident activity.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? OrganizationId { get; set; }

    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

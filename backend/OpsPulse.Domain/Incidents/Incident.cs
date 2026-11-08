using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Incidents;

public enum IncidentSeverity
{
    Informational = 0,
    Warning = 1,
    Critical = 2
}

public enum IncidentStatus
{
    Open = 0,
    Acknowledged = 1,
    Resolved = 2
}

/// <summary>
/// An incident raised automatically by alert rules or manually.
/// <see cref="DeduplicationKey"/> prevents duplicate incidents for the same rule + source.
/// </summary>
public class Incident : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Warning;

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public string SourceType { get; set; } = string.Empty;

    public string? SourceName { get; set; }

    /// <summary>Stable key used to deduplicate recurring alerts, e.g. "rule:{id}:service:{id}".</summary>
    public string? DeduplicationKey { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime? AcknowledgedAtUtc { get; set; }

    public Guid? AcknowledgedByUserId { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public DateTime LastEventAtUtc { get; set; }

    public ICollection<IncidentEvent> Events { get; set; } = [];
}

public enum IncidentEventType
{
    Created = 0,
    StatusChanged = 1,
    Comment = 2,
    Recurring = 3
}

public class IncidentEvent : BaseEntity
{
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public IncidentEventType Type { get; set; }

    public string Message { get; set; } = string.Empty;

    public Guid? ActorUserId { get; set; }

    public string? ActorName { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}

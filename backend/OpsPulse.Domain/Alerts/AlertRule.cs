using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Alerts;

public enum AlertMetric
{
    ServiceDown = 0,
    ResponseTime = 1,
    CpuUsage = 2,
    MemoryUsage = 3,
    DiskUsage = 4,
    DatabaseConnectionUtilization = 5,
    AgentHeartbeat = 6
}

public enum AlertComparator
{
    GreaterThan = 0,
    LessThan = 1
}

public enum AlertSeverity
{
    Informational = 0,
    Warning = 1,
    Critical = 2
}

/// <summary>
/// A threshold rule evaluated by the monitoring worker. When a rule breaches,
/// an incident is created (deduplicated) and notifications are dispatched.
/// </summary>
public class AlertRule : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public AlertMetric Metric { get; set; } = AlertMetric.CpuUsage;

    public AlertComparator Comparator { get; set; } = AlertComparator.GreaterThan;

    public double Threshold { get; set; }

    /// <summary>How long the condition must hold before triggering (0 = immediately).</summary>
    public int DurationMinutes { get; set; }

    public int CooldownMinutes { get; set; } = 15;

    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    /// <summary>Optional service scope; null applies to all services in the organization.</summary>
    public Guid? TargetServiceId { get; set; }

    public bool IsEnabled { get; set; } = true;
}

public enum ChannelType
{
    InApp = 0,
    Email = 1,
    Webhook = 2
}

public class AlertChannel : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public ChannelType Type { get; set; } = ChannelType.InApp;

    /// <summary>Email address or webhook URL depending on the channel type.</summary>
    public string? Target { get; set; }

    public bool IsEnabled { get; set; } = true;
}

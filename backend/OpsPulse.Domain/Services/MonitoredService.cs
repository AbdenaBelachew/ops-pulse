using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Services;

/// <summary>
/// An externally hosted application or API endpoint that OpsPulse monitors
/// with periodic HTTP checks.
/// </summary>
public class MonitoredService : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ServiceEnvironment Environment { get; set; } = ServiceEnvironment.Production;

    public int CheckIntervalSeconds { get; set; } = 60;

    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Comma-separated HTTP status codes considered healthy (e.g. "200,204").</summary>
    public string ExpectedStatusCodes { get; set; } = "200";

    public bool IsActive { get; set; } = true;

    // Denormalized status maintained by the check engine.
    public ServiceStatus CurrentStatus { get; set; } = ServiceStatus.Unknown;

    public DateTime? LastCheckedAtUtc { get; set; }

    public int? LastResponseTimeMs { get; set; }

    public int ConsecutiveFailures { get; set; }

    public ICollection<ServiceCheck> Checks { get; set; } = [];
}

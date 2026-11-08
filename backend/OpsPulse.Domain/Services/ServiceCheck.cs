using OpsPulse.Domain.Common;

namespace OpsPulse.Domain.Services;

/// <summary>
/// One executed HTTP check for a monitored service — the monitoring history.
/// </summary>
public class ServiceCheck : BaseEntity
{
    public Guid ServiceId { get; set; }

    public MonitoredService Service { get; set; } = null!;

    public DateTime CheckedAtUtc { get; set; }

    public bool IsSuccess { get; set; }

    public int? StatusCode { get; set; }

    public int ResponseTimeMs { get; set; }

    public string? ErrorMessage { get; set; }

    public ServiceStatus StatusAfter { get; set; }
}

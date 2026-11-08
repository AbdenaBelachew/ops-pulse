using OpsPulse.Domain.Common;

namespace OpsPulse.Domain.Servers;

/// <summary>
/// A point-in-time resource usage sample collected by an agent.
/// </summary>
public class ServerMetric : BaseEntity
{
    public Guid ServerId { get; set; }

    public Server Server { get; set; } = null!;

    public DateTime CollectedAtUtc { get; set; }

    public double CpuUsagePercent { get; set; }

    public long MemoryUsedMb { get; set; }

    public long MemoryTotalMb { get; set; }

    public double? DiskUsedPercent { get; set; }

    public double? NetworkInMbps { get; set; }

    public double? NetworkOutMbps { get; set; }
}

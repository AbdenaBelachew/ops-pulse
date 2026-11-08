using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Servers;

public enum ServerStatus
{
    Unknown = 0,
    Online = 1,
    Offline = 2
}

/// <summary>
/// A machine running the OpsPulse monitoring agent.
/// </summary>
public class Server : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Hostname { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? OsName { get; set; }

    public string? OsVersion { get; set; }

    public string? AgentVersion { get; set; }

    public int CpuCores { get; set; }

    public long TotalMemoryMb { get; set; }

    public ServerStatus Status { get; set; } = ServerStatus.Unknown;

    public DateTime? LastHeartbeatAtUtc { get; set; }

    public double? LastCpuUsagePercent { get; set; }

    public double? LastMemoryUsagePercent { get; set; }

    public ICollection<ServerMetric> Metrics { get; set; } = [];
}

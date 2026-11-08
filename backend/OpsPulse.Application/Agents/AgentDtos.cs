namespace OpsPulse.Application.Agents;

public sealed record AgentHeartbeatRequest(
    string Hostname,
    string? Name,
    string? IpAddress,
    string? OsName,
    string? OsVersion,
    string? AgentVersion,
    int CpuCores,
    long TotalMemoryMb);

public sealed record AgentMetricSample(
    DateTime CollectedAtUtc,
    double CpuUsagePercent,
    long MemoryUsedMb,
    long MemoryTotalMb,
    double? DiskUsedPercent,
    double? NetworkInMbps,
    double? NetworkOutMbps);

public sealed record AgentMetricsBatchRequest(Guid ServerId, IReadOnlyList<AgentMetricSample> Metrics);

public sealed record AgentHeartbeatResponse(Guid ServerId, int RecommendedIntervalSeconds);

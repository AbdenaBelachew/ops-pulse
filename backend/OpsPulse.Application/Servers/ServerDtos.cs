using FluentValidation;
using OpsPulse.Domain.Servers;

namespace OpsPulse.Application.Servers;

public sealed record ServerDto(
    Guid Id,
    string Name,
    string Hostname,
    string? IpAddress,
    string? OsName,
    string? OsVersion,
    string? AgentVersion,
    int CpuCores,
    long TotalMemoryMb,
    ServerStatus Status,
    DateTime? LastHeartbeatAtUtc,
    double? LastCpuUsagePercent,
    double? LastMemoryUsagePercent,
    DateTime CreatedAtUtc);

public sealed record ServerMetricDto(
    DateTime CollectedAtUtc,
    double CpuUsagePercent,
    long MemoryUsedMb,
    long MemoryTotalMb,
    double? DiskUsedPercent,
    double? NetworkInMbps,
    double? NetworkOutMbps);

public sealed record ServerDetailsDto(
    ServerDto Server,
    IReadOnlyList<ServerMetricDto> RecentMetrics);

public sealed record ServerListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ServerStatus? Status = null);

public sealed record UpdateServerRequest(string? Name = null);

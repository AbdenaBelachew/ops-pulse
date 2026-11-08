using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Domain.Containers;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Monitoring;

/// <summary>
/// Synchronizes containers and resource stats from a Docker Engine endpoint
/// (named pipe, unix socket or TCP).
/// </summary>
public sealed class DockerSyncEngine(OpsPulseDbContext db, ILogger<DockerSyncEngine> logger)
{
    public async Task<bool> SyncAsync(DockerHost host, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new DockerClientConfiguration(ToUri(host.Endpoint)).CreateClient();
            await client.System.PingAsync(cancellationToken);

            var containers = await client.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, cancellationToken);

            var now = DateTime.UtcNow;
            var statsById = new Dictionary<string, ContainerStatsResponse>();

            foreach (var container in containers.Take(50))
            {
                try
                {
                    await using var stream = await client.Containers.GetContainerStatsAsync(
                        container.ID,
                        new ContainerStatsParameters { Stream = false, OneShot = true },
                        cancellationToken);
                    var stats = await System.Text.Json.JsonSerializer.DeserializeAsync<ContainerStatsResponse>(
                        stream, cancellationToken: cancellationToken);
                    if (stats is not null)
                    {
                        statsById[container.ID] = stats;
                    }
                }
                catch (Exception exception)
                {
                    logger.LogDebug(exception, "Stats unavailable for container {Container}", container.ID[..12]);
                }
            }

            var existing = await db.DockerContainers
                .Where(c => c.DockerHostId == host.Id)
                .ToListAsync(cancellationToken);
            db.DockerContainers.RemoveRange(existing);

            foreach (var container in containers)
            {
                double? cpuPercent = null;
                long? memoryUsedMb = null;
                long? memoryLimitMb = null;

                if (statsById.TryGetValue(container.ID, out var stats))
                {
                    cpuPercent = ComputeCpuPercent(stats);
                    memoryUsedMb = ComputeMemoryUsedMb(stats);
                    memoryLimitMb = stats.MemoryStats is { } memory && memory.Limit > 0
                        ? (long)memory.Limit / (1024 * 1024)
                        : null;
                }

                db.DockerContainers.Add(new DockerContainer
                {
                    DockerHostId = host.Id,
                    ContainerId = container.ID,
                    Name = container.Names?.FirstOrDefault()?.TrimStart('/') ?? container.ID[..12],
                    Image = container.Image ?? "unknown",
                    State = container.State ?? "unknown",
                    Status = container.Status ?? string.Empty,
                    CpuUsagePercent = cpuPercent,
                    MemoryUsedMb = memoryUsedMb,
                    MemoryLimitMb = memoryLimitMb,
                    StartedAtUtc = container.Created,
                    SyncedAtUtc = now
                });
            }

            host.IsReachable = true;
            host.ContainerCount = containers.Count;
            host.RunningCount = containers.Count(c => c.State == "running");
            host.LastSyncedAtUtc = now;
            host.LastError = null;

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Docker host {Host} synced: {Containers} containers", host.Name, containers.Count);
            return true;
        }
        catch (Exception exception)
        {
            host.IsReachable = false;
            host.LastError = exception.Message;
            host.LastSyncedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Docker sync failed for {Host}: {Message}", host.Name, exception.Message);
            return false;
        }
    }

    private static double? ComputeCpuPercent(ContainerStatsResponse? stats)
    {
        if (stats?.CPUStats == null || stats.PreCPUStats?.CPUUsage == null)
        {
            return null;
        }

        var cpuDelta = (double)stats.CPUStats.CPUUsage.TotalUsage - (double)stats.PreCPUStats.CPUUsage.TotalUsage;
        var systemDelta = (double)stats.CPUStats.SystemUsage - (double)stats.PreCPUStats.SystemUsage;
        if (systemDelta <= 0 || cpuDelta < 0)
        {
            return null;
        }

        var onlineCpus = stats.CPUStats.OnlineCPUs > 0
            ? (double)stats.CPUStats.OnlineCPUs
            : stats.PreCPUStats.OnlineCPUs > 0 ? (double)stats.PreCPUStats.OnlineCPUs : 1;

        return Math.Round(cpuDelta / systemDelta * onlineCpus * 100.0, 2);
    }

    private static long? ComputeMemoryUsedMb(ContainerStatsResponse? stats)
    {
        var memory = stats?.MemoryStats;
        if (memory is null || memory.Usage <= 0)
        {
            return null;
        }

        var cache = 0L;
        if (memory.Stats is not null)
        {
            if (memory.Stats.TryGetValue("inactive_file", out var inactiveFile))
            {
                cache = ToLong(inactiveFile);
            }
            else if (memory.Stats.TryGetValue("cache", out var cacheValue))
            {
                cache = ToLong(cacheValue);
            }
        }

        return ((long)memory.Usage - cache) / (1024 * 1024);
    }

    private static long ToLong(object? value) => value switch
    {
        long l => l,
        ulong ul => (long)ul,
        int i => i,
        double d => (long)d,
        _ => 0
    };

    private static Uri ToUri(string endpoint) => endpoint switch
    {
        var e when e.StartsWith("npipe://", StringComparison.OrdinalIgnoreCase) => new Uri(e),
        var e when e.StartsWith("unix://", StringComparison.OrdinalIgnoreCase) => new Uri(e),
        var e when e.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase) => new Uri(e.Replace("tcp://", "http://", StringComparison.OrdinalIgnoreCase)),
        var e when e.StartsWith("http://", StringComparison.OrdinalIgnoreCase) => new Uri(e),
        var e when e.StartsWith("https://", StringComparison.OrdinalIgnoreCase) => new Uri(e),
        _ => new Uri("npipe://./pipe/docker_engine")
    };
}

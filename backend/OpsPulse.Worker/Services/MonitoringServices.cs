using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Servers;
using OpsPulse.Infrastructure.Monitoring;
using OpsPulse.Infrastructure.Persistence;
using OpsPulse.Infrastructure.Services;

namespace OpsPulse.Worker.Services;

/// <summary>
/// Milestone 4 core: periodically executes due HTTP checks with bounded
/// concurrency, one scope per check.
/// </summary>
public sealed class ServiceCheckScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<ServiceCheckScheduler> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxConcurrency = 8;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Service check scheduler started (interval 10s)");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                List<Domain.Services.MonitoredService> dueServices;
                using (var scope = scopeFactory.CreateScope())
                {
                    var processor = scope.ServiceProvider.GetRequiredService<ServiceCheckProcessor>();
                    dueServices = await processor.GetDueServicesAsync(BatchSize, stoppingToken);
                }

                if (dueServices.Count == 0)
                {
                    continue;
                }

                using var gate = new SemaphoreSlim(MaxConcurrency);
                var tasks = dueServices.Select(async service =>
                {
                    await gate.WaitAsync(stoppingToken);
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var processor = scope.ServiceProvider.GetRequiredService<ServiceCheckProcessor>();
                        await processor.ProcessAsync(service, stoppingToken);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Check failed for service {Service}", service.Name);
                    }
                    finally
                    {
                        gate.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Service check scheduler tick failed");
            }
        }
    }
}

/// <summary>Marks servers offline when their agent heartbeat goes stale.</summary>
public sealed class AgentHeartbeatMonitor(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<AgentHeartbeatMonitor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OpsPulseDbContext>();
                var notifier = scope.ServiceProvider.GetRequiredService<IRealTimeNotifier>();

                var cutoff = DateTime.UtcNow.AddSeconds(-options.Value.AgentOfflineAfterSeconds);
                var staleServers = await db.Servers
                    .Where(s => s.Status == ServerStatus.Online && s.LastHeartbeatAtUtc < cutoff)
                    .ToListAsync(stoppingToken);

                foreach (var server in staleServers)
                {
                    server.Status = ServerStatus.Offline;
                    await notifier.AgentDisconnectedAsync(server.OrganizationId,
                        new AgentEventPayload(server.Id, server.Name, DateTime.UtcNow), stoppingToken);
                }

                if (staleServers.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Marked {Count} server(s) offline (stale heartbeat)", staleServers.Count);
                }

                // Evaluate "Agent heartbeat missing" rules for every offline server on each
                // tick so sustained-duration windows can elapse. Recovery is evaluated by the
                // API when the agent reports again.
                var evaluator = scope.ServiceProvider.GetRequiredService<IAlertEvaluator>();
                var offlineServers = await db.Servers
                    .AsNoTracking()
                    .Where(s => s.Status == ServerStatus.Offline)
                    .Select(s => new { s.Id, s.OrganizationId, s.Name, s.LastHeartbeatAtUtc })
                    .ToListAsync(stoppingToken);

                foreach (var server in offlineServers)
                {
                    await evaluator.EvaluateHealthAsync(server.OrganizationId, AlertMetric.AgentHeartbeat,
                        isHealthy: false, "server", server.Id, server.Name,
                        $"no heartbeat since {server.LastHeartbeatAtUtc:u}", stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Agent heartbeat monitor tick failed");
            }
        }
    }
}

/// <summary>Collects database metrics for every active connection.</summary>
public sealed class DatabaseCheckScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseCheckScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Database check scheduler started (interval 60s)");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OpsPulseDbContext>();
                var processor = scope.ServiceProvider.GetRequiredService<DatabaseCheckProcessor>();

                var connections = await db.DatabaseConnections
                    .Where(c => c.IsActive)
                    .ToListAsync(stoppingToken);

                foreach (var connection in connections)
                {
                    await processor.ProcessAsync(connection, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Database check scheduler tick failed");
            }
        }
    }
}

/// <summary>Synchronizes Kubernetes clusters every 60 seconds.</summary>
public sealed class KubernetesSyncScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<KubernetesSyncScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Kubernetes sync scheduler started (interval 60s)");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OpsPulseDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<KubernetesSyncEngine>();

                var clusters = await db.KubernetesClusters
                    .Where(c => c.IsActive)
                    .ToListAsync(stoppingToken);

                foreach (var cluster in clusters)
                {
                    await engine.SyncAsync(cluster, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Kubernetes sync scheduler tick failed");
            }
        }
    }
}

/// <summary>Synchronizes Docker hosts every 30 seconds.</summary>
public sealed class DockerSyncScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<DockerSyncScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Docker sync scheduler started (interval 30s)");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OpsPulseDbContext>();
                var engine = scope.ServiceProvider.GetRequiredService<DockerSyncEngine>();

                var hosts = await db.DockerHosts
                    .Where(h => h.IsActive)
                    .ToListAsync(stoppingToken);

                foreach (var host in hosts)
                {
                    await engine.SyncAsync(host, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Docker sync scheduler tick failed");
            }
        }
    }
}

/// <summary>Applies data retention policy: removes monitoring history past the window.</summary>
public sealed class RetentionService(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<RetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OpsPulseDbContext>();
                var cutoff = DateTime.UtcNow.AddDays(-options.Value.RetentionDays);

                var checks = await db.ServiceChecks.Where(c => c.CheckedAtUtc < cutoff).CountAsync(stoppingToken);
                db.ServiceChecks.RemoveRange(db.ServiceChecks.Where(c => c.CheckedAtUtc < cutoff));
                db.ServerMetrics.RemoveRange(db.ServerMetrics.Where(m => m.CollectedAtUtc < cutoff));
                db.DatabaseMetrics.RemoveRange(db.DatabaseMetrics.Where(m => m.CollectedAtUtc < cutoff));
                db.SystemLogs.RemoveRange(db.SystemLogs.Where(l => l.TimestampUtc < cutoff));

                var removed = await db.SaveChangesAsync(stoppingToken);
                if (removed > 0)
                {
                    logger.LogInformation("Retention: removed {Count} monitoring rows older than {Days}d", removed, options.Value.RetentionDays);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Retention tick failed");
            }
        }
    }
}

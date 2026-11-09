using OpsPulse.Application.Common;
using OpsPulse.Application.Databases;
using OpsPulse.Application.Interfaces;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Databases;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Monitoring;

/// <summary>
/// Runs one database probe end to end: probe, store the metric sample, update
/// health, broadcast changes and evaluate connection-utilization rules. Shared by
/// the Worker's scheduler and the API's "check now".
/// </summary>
public sealed class DatabaseCheckProcessor(
    OpsPulseDbContext db,
    DatabaseCheckEngine engine,
    IAlertEvaluator evaluator,
    IRealTimeNotifier notifier)
{
    /// <summary>Probes slower than this mark the database Degraded.</summary>
    public const int DegradedResponseTimeMs = 2000;

    /// <param name="connection">A tracked entity from <paramref name="db"/>'s context.</param>
    public async Task<DatabaseMetricDto> ProcessAsync(DatabaseConnection connection, CancellationToken cancellationToken = default)
    {
        var result = await engine.CheckAsync(connection, cancellationToken);
        var now = DateTime.UtcNow;

        var metric = new DatabaseMetric
        {
            DatabaseConnectionId = connection.Id,
            CollectedAtUtc = now,
            IsReachable = result.IsReachable,
            ActiveConnections = result.ActiveConnections,
            MaxConnections = result.MaxConnections,
            DatabaseSizeMb = result.SizeMb,
            ResponseTimeMs = result.ResponseTimeMs,
            ErrorMessage = result.ErrorMessage
        };
        db.DatabaseMetrics.Add(metric);

        var previous = connection.Health;
        connection.Health = !result.IsReachable
            ? DatabaseHealth.Unreachable
            : result.ResponseTimeMs > DegradedResponseTimeMs
                ? DatabaseHealth.Degraded
                : DatabaseHealth.Healthy;
        connection.LastCheckedAtUtc = now;

        await db.SaveChangesAsync(cancellationToken);

        if (previous != connection.Health)
        {
            await notifier.MetricsUpdatedAsync(connection.OrganizationId,
                new MetricsUpdatedPayload("database", connection.Id, now), cancellationToken);
        }

        if (result.IsReachable && result.ActiveConnections is { } active && result.MaxConnections is { } max && max > 0)
        {
            await evaluator.EvaluateMetricAsync(connection.OrganizationId,
                AlertMetric.DatabaseConnectionUtilization,
                active * 100.0 / max, "database", connection.Id, connection.Name, cancellationToken);
        }

        return new DatabaseMetricDto(metric.CollectedAtUtc, metric.IsReachable, metric.ActiveConnections,
            metric.MaxConnections, metric.DatabaseSizeMb, metric.ResponseTimeMs, metric.ErrorMessage);
    }
}

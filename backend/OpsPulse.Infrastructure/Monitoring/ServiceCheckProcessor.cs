using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Services;
using OpsPulse.Infrastructure.Monitoring;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

/// <summary>
/// Runs a single service check and applies the full pipeline: record history,
/// update denormalized status, broadcast real-time events and evaluate alerts.
/// Used by the worker scheduler and by the manual "check now" API.
/// </summary>
public sealed class ServiceCheckProcessor(
    OpsPulseDbContext db,
    SafeHttpCheckEngine engine,
    IAlertEvaluator alertEvaluator,
    IRealTimeNotifier notifier,
    ILogger<ServiceCheckProcessor> logger)
{
    public async Task<ServiceCheck> ProcessAsync(MonitoredService service, CancellationToken cancellationToken = default)
    {
        var result = await engine.CheckAsync(service, cancellationToken);
        var now = DateTime.UtcNow;

        var previousStatus = service.CurrentStatus;
        var check = new ServiceCheck
        {
            ServiceId = service.Id,
            CheckedAtUtc = now,
            IsSuccess = result.IsSuccess,
            StatusCode = result.StatusCode,
            ResponseTimeMs = result.ResponseTimeMs,
            ErrorMessage = result.ErrorMessage,
            StatusAfter = result.StatusAfter
        };

        db.ServiceChecks.Add(check);
        service.CurrentStatus = result.StatusAfter;
        service.LastCheckedAtUtc = now;
        service.LastResponseTimeMs = result.ResponseTimeMs;
        service.ConsecutiveFailures = result.IsSuccess ? 0 : service.ConsecutiveFailures + 1;

        await db.SaveChangesAsync(cancellationToken);

        if (previousStatus != service.CurrentStatus)
        {
            logger.LogInformation(
                "Service {Service} transitioned {Previous} -> {Current} ({Status code} in {ResponseTime} ms)",
                service.Name, previousStatus, service.CurrentStatus, result.StatusCode, result.ResponseTimeMs);

            await notifier.ServiceStatusChangedAsync(
                service.OrganizationId,
                new ServiceStatusPayload(service.Id, service.Name, service.CurrentStatus.ToString(), result.ResponseTimeMs, now),
                cancellationToken);
        }

        await alertEvaluator.EvaluateHealthAsync(
            service.OrganizationId, AlertMetric.ServiceDown, result.IsSuccess,
            "service", service.Id, service.Name, result.ErrorMessage, cancellationToken);

        if (result.IsSuccess)
        {
            await alertEvaluator.EvaluateMetricAsync(
                service.OrganizationId, AlertMetric.ResponseTime, result.ResponseTimeMs,
                "service", service.Id, service.Name, cancellationToken);
        }

        return check;
    }

    /// <summary>Returns the IDs of services whose next check is due. Called by the scheduler.</summary>
    public async Task<List<MonitoredService>> GetDueServicesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var dueIds = await db.MonitoredServices
            .AsNoTracking()
            .Where(s => s.IsActive &&
                (s.LastCheckedAtUtc == null ||
                    s.LastCheckedAtUtc.Value.AddSeconds(s.CheckIntervalSeconds) <= now))
            .OrderBy(s => s.LastCheckedAtUtc ?? DateTime.MinValue)
            .Take(batchSize)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (dueIds.Count == 0)
        {
            return [];
        }

        return await db.MonitoredServices
            .Where(s => dueIds.Contains(s.Id))
            .ToListAsync(cancellationToken);
    }
}

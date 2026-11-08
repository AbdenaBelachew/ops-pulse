using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Domain.Incidents;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Monitoring;

/// <summary>
/// Persists incidents and their timeline events, broadcasting real-time updates.
/// </summary>
public sealed class IncidentManager(
    OpsPulseDbContext db,
    IRealTimeNotifier notifier,
    ILogger<IncidentManager> logger) : IIncidentManager
{
    public async Task<Incident> CreateAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        incident.Events.Add(new IncidentEvent
        {
            Type = IncidentEventType.Created,
            Message = $"Incident opened: {incident.Description ?? incident.Title}",
            OccurredAtUtc = DateTime.UtcNow
        });

        db.Incidents.Add(incident);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Incident {IncidentId} created [{Severity}] {Title}",
            incident.Id, incident.Severity, incident.Title);

        await notifier.IncidentCreatedAsync(
            incident.OrganizationId, ToPayload(incident), cancellationToken);

        return incident;
    }

    public Task<Incident?> FindOpenByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken = default)
        => db.Incidents
            .FirstOrDefaultAsync(i => i.DeduplicationKey == deduplicationKey && i.Status != IncidentStatus.Resolved, cancellationToken);

    public async Task RecordRecurringAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        incident.Events.Add(new IncidentEvent
        {
            Type = IncidentEventType.Recurring,
            Message = "Condition breached again while incident is open.",
            OccurredAtUtc = DateTime.UtcNow
        });
        incident.LastEventAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await notifier.IncidentUpdatedAsync(incident.OrganizationId, ToPayload(incident), cancellationToken);
    }

    public async Task<Incident> ResolveAsync(
        Incident incident,
        Guid? actorUserId,
        string? actorName,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedAtUtc = now;
        incident.ResolvedByUserId = actorUserId;
        incident.LastEventAtUtc = now;

        incident.Events.Add(new IncidentEvent
        {
            Type = IncidentEventType.StatusChanged,
            Message = $"Resolved by {actorName ?? "system"}: {reason}",
            ActorUserId = actorUserId,
            ActorName = actorName,
            OccurredAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Incident {IncidentId} resolved: {Reason}", incident.Id, reason);
        await notifier.IncidentResolvedAsync(incident.OrganizationId, ToPayload(incident), cancellationToken);

        return incident;
    }

    public static IncidentPayload ToPayload(Incident incident) =>
        new(incident.Id, incident.Title, incident.Severity.ToString(), incident.Status.ToString(),
            incident.SourceName, incident.StartedAtUtc);
}

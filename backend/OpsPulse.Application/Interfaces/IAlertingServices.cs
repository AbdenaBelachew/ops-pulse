using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Incidents;

namespace OpsPulse.Application.Interfaces;

/// <summary>
/// Evaluates alert rules against incoming metric/health values, manages
/// deduplicated incidents and dispatches notifications.
/// </summary>
public interface IAlertEvaluator
{
    /// <summary>Evaluates a numeric metric (ms, %, etc.) against GreaterThan/LessThan thresholds.</summary>
    Task EvaluateMetricAsync(
        Guid organizationId,
        AlertMetric metric,
        double value,
        string sourceType,
        Guid? sourceId,
        string sourceName,
        CancellationToken cancellationToken = default);

    /// <summary>Evaluates a boolean health signal (ServiceDown, AgentHeartbeat).</summary>
    Task EvaluateHealthAsync(
        Guid organizationId,
        AlertMetric metric,
        bool isHealthy,
        string sourceType,
        Guid? sourceId,
        string sourceName,
        string? detail = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Creates and transitions incidents. Used by the alert evaluator and by the API.
/// </summary>
public interface IIncidentManager
{
    Task<Incident> CreateAsync(Incident incident, CancellationToken cancellationToken = default);

    Task<Incident?> FindOpenByDeduplicationKeyAsync(string deduplicationKey, CancellationToken cancellationToken = default);

    Task RecordRecurringAsync(Incident incident, CancellationToken cancellationToken = default);

    Task<Incident> ResolveAsync(Incident incident, Guid? actorUserId, string? actorName, string reason, CancellationToken cancellationToken = default);
}

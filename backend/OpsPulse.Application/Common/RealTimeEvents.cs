namespace OpsPulse.Application.Common;

public sealed record ServiceStatusPayload(
    Guid ServiceId,
    string Name,
    string Status,
    int? ResponseTimeMs,
    DateTime TimestampUtc);

public sealed record IncidentPayload(
    Guid IncidentId,
    string Title,
    string Severity,
    string Status,
    string? SourceName,
    DateTime StartedAtUtc);

public sealed record MetricsUpdatedPayload(
    string Kind,
    Guid? SourceId,
    DateTime TimestampUtc);

public sealed record AgentEventPayload(
    Guid ServerId,
    string Name,
    DateTime TimestampUtc);

/// <summary>
/// Publishes real-time events to organization-scoped SignalR groups through the
/// Redis backplane, so the Worker and API processes reach the same clients.
/// </summary>
public interface IRealTimeNotifier
{
    Task ServiceStatusChangedAsync(Guid organizationId, ServiceStatusPayload payload, CancellationToken cancellationToken = default);
    Task IncidentCreatedAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default);
    Task IncidentUpdatedAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default);
    Task IncidentResolvedAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default);
    Task MetricsUpdatedAsync(Guid organizationId, MetricsUpdatedPayload payload, CancellationToken cancellationToken = default);
    Task AgentConnectedAsync(Guid organizationId, AgentEventPayload payload, CancellationToken cancellationToken = default);
    Task AgentDisconnectedAsync(Guid organizationId, AgentEventPayload payload, CancellationToken cancellationToken = default);
    Task AlertTriggeredAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default);
}

public sealed record AuditEntry(
    string Action,
    Guid? OrganizationId = null,
    Guid? UserId = null,
    string? UserEmail = null,
    string? EntityType = null,
    string? EntityId = null,
    string? Details = null,
    string? IpAddress = null);

public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Symmetric encryption for stored credentials (database passwords, kubeconfigs).
/// The key comes from platform configuration and is never stored in the database.
/// </summary>
public interface ICredentialProtector
{
    string Protect(string plainText);

    string Unprotect(string cipherText);
}

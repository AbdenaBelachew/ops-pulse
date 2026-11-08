using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Common;

namespace OpsPulse.Infrastructure.RealTime;

/// <summary>
/// Publishes events through the Redis backplane so that both the API process
/// (which owns client connections) and the Worker process reach every client.
/// </summary>
public sealed class SignalRNotifier(IHubContext<MonitoringHub> hubContext, ILogger<SignalRNotifier> logger) : IRealTimeNotifier
{
    public Task ServiceStatusChangedAsync(Guid organizationId, ServiceStatusPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "ServiceStatusChanged", payload, cancellationToken);

    public Task IncidentCreatedAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "IncidentCreated", payload, cancellationToken);

    public Task IncidentUpdatedAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "IncidentUpdated", payload, cancellationToken);

    public Task IncidentResolvedAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "IncidentResolved", payload, cancellationToken);

    public Task MetricsUpdatedAsync(Guid organizationId, MetricsUpdatedPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "MetricsUpdated", payload, cancellationToken);

    public Task AgentConnectedAsync(Guid organizationId, AgentEventPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "AgentConnected", payload, cancellationToken);

    public Task AgentDisconnectedAsync(Guid organizationId, AgentEventPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "AgentDisconnected", payload, cancellationToken);

    public Task AlertTriggeredAsync(Guid organizationId, IncidentPayload payload, CancellationToken cancellationToken = default)
        => BroadcastAsync(MonitoringHub.GroupName(organizationId), "AlertTriggered", payload, cancellationToken);

    private async Task BroadcastAsync(string groupName, string eventName, object payload, CancellationToken cancellationToken)
    {
        try
        {
            await hubContext.Clients.Groups(groupName, MonitoringHub.PlatformGroup)
                .SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception exception)
        {
            // Real-time delivery must never break the monitoring pipeline.
            logger.LogWarning(exception, "Failed to broadcast {EventName} to {Group}", eventName, groupName);
        }
    }
}

using OpsPulse.Application.Agents;
using OpsPulse.Application.Alerts;
using OpsPulse.Application.Audit;
using OpsPulse.Application.Common;
using OpsPulse.Application.Databases;
using OpsPulse.Application.Incidents;
using OpsPulse.Application.InfrastructureMonitor;
using OpsPulse.Application.Notifications;
using OpsPulse.Application.Reports;
using OpsPulse.Application.Security;
using OpsPulse.Application.Servers;
using OpsPulse.Application.Services;

namespace OpsPulse.Application.Interfaces;

public interface IServiceService
{
    Task<PagedResult<ServiceDto>> ListAsync(ServiceListQuery query, CancellationToken cancellationToken = default);
    Task<ServiceDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceCheckDto> RunCheckAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IServerService
{
    Task<PagedResult<ServerDto>> ListAsync(ServerListQuery query, CancellationToken cancellationToken = default);
    Task<ServerDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServerDto> UpdateAsync(Guid id, UpdateServerRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IDatabaseService
{
    Task<PagedResult<DatabaseConnectionDto>> ListAsync(DatabaseListQuery query, CancellationToken cancellationToken = default);
    Task<DatabaseDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DatabaseConnectionDto> CreateAsync(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);
    Task<DatabaseConnectionDto> UpdateAsync(Guid id, UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IKubernetesService
{
    Task<IReadOnlyList<KubernetesClusterDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<KubernetesClusterDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KubernetesClusterDto> CreateAsync(CreateKubernetesClusterRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KubernetesClusterDto> SyncAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IDockerService
{
    Task<IReadOnlyList<DockerHostDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<DockerHostDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DockerHostDto> CreateAsync(CreateDockerHostRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DockerHostDto> SyncAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAlertRuleService
{
    Task<IReadOnlyList<AlertRuleDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AlertRuleDto> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken = default);
    Task<AlertRuleDto> UpdateAsync(Guid id, UpdateAlertRuleRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AlertChannelDto> CreateChannelAsync(CreateAlertChannelRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertChannelDto>> ListChannelsAsync(CancellationToken cancellationToken = default);
    Task DeleteChannelAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IIncidentService
{
    Task<PagedResult<IncidentDto>> ListAsync(IncidentListQuery query, CancellationToken cancellationToken = default);
    Task<IncidentDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IncidentDto> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IncidentDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IncidentDto> AddCommentAsync(Guid id, AddIncidentCommentRequest request, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> ListAsync(int page, int pageSize, bool? unreadOnly, CancellationToken cancellationToken = default);
    Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<IReadOnlyList<UptimeReportRow>> UptimeAsync(int hours, CancellationToken cancellationToken = default);
    Task<IncidentSummaryRow> IncidentsAsync(int hours, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServerUtilizationRow>> ServerUtilizationAsync(int hours, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}

public interface IApiKeyService
{
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<ApiKeyCreatedDto> CreateAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAgentIngestService
{
    Task<AgentHeartbeatResponse> HeartbeatAsync(AgentHeartbeatRequest request, CancellationToken cancellationToken = default);
    Task SubmitMetricsAsync(AgentMetricsBatchRequest request, CancellationToken cancellationToken = default);
}

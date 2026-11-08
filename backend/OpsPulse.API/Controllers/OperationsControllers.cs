using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Agents;
using OpsPulse.Application.Alerts;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.Common;
using OpsPulse.Application.Incidents;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Notifications;
using OpsPulse.Application.Reports;
using OpsPulse.Application.Security;
using OpsPulse.Application.Audit;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Notifications;
using OpsPulse.Infrastructure.Security;

namespace OpsPulse.API.Controllers;

[ApiController]
[Route("api/v1/alert-rules")]
[Authorize]
[Produces("application/json")]
public sealed class AlertRulesController(IAlertRuleService alertRuleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlertRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertRuleDto>>> List(CancellationToken cancellationToken)
        => Ok(await alertRuleService.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(AlertRuleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AlertRuleDto>> Create(CreateAlertRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await alertRuleService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, rule);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(AlertRuleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertRuleDto>> Update(Guid id, UpdateAlertRuleRequest request, CancellationToken cancellationToken)
        => Ok(await alertRuleService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await alertRuleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("channels")]
    [ProducesResponseType(typeof(IReadOnlyList<AlertChannelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AlertChannelDto>>> ListChannels(CancellationToken cancellationToken)
        => Ok(await alertRuleService.ListChannelsAsync(cancellationToken));

    [HttpPost("channels")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(AlertChannelDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AlertChannelDto>> CreateChannel(CreateAlertChannelRequest request, CancellationToken cancellationToken)
    {
        var channel = await alertRuleService.CreateChannelAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, channel);
    }

    [HttpDelete("channels/{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteChannel(Guid id, CancellationToken cancellationToken)
    {
        await alertRuleService.DeleteChannelAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/incidents")]
[Authorize]
[Produces("application/json")]
public sealed class IncidentsController(IIncidentService incidentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IncidentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<IncidentDto>>> List(
        [FromQuery] IncidentListQuery query, CancellationToken cancellationToken)
        => Ok(await incidentService.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IncidentDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IncidentDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await incidentService.GetAsync(id, cancellationToken));

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Policy = Policies.CanManageIncidents)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IncidentDto>> Acknowledge(Guid id, CancellationToken cancellationToken)
        => Ok(await incidentService.AcknowledgeAsync(id, cancellationToken));

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Policy = Policies.CanManageIncidents)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IncidentDto>> Resolve(Guid id, CancellationToken cancellationToken)
        => Ok(await incidentService.ResolveAsync(id, cancellationToken));

    [HttpPost("{id:guid}/comments")]
    [Authorize(Policy = Policies.CanManageIncidents)]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<IncidentDto>> Comment(Guid id, AddIncidentCommentRequest request, CancellationToken cancellationToken)
        => Ok(await incidentService.AddCommentAsync(id, request, cancellationToken));
}

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
[Produces("application/json")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? unreadOnly = null,
        CancellationToken cancellationToken = default)
        => Ok(await notificationService.ListAsync(page, pageSize, unreadOnly, cancellationToken));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountDto>> UnreadCount(CancellationToken cancellationToken)
        => Ok(await notificationService.GetUnreadCountAsync(cancellationToken));

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllReadAsync(cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/reports")]
[Authorize]
[Produces("application/json")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("uptime")]
    [ProducesResponseType(typeof(IReadOnlyList<UptimeReportRow>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UptimeReportRow>>> Uptime(
        [FromQuery] int hours = 24, CancellationToken cancellationToken = default)
        => Ok(await reportService.UptimeAsync(hours, cancellationToken));

    [HttpGet("incidents")]
    [ProducesResponseType(typeof(IncidentSummaryRow), StatusCodes.Status200OK)]
    public async Task<ActionResult<IncidentSummaryRow>> Incidents(
        [FromQuery] int hours = 24, CancellationToken cancellationToken = default)
        => Ok(await reportService.IncidentsAsync(hours, cancellationToken));

    [HttpGet("servers")]
    [ProducesResponseType(typeof(IReadOnlyList<ServerUtilizationRow>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ServerUtilizationRow>>> Servers(
        [FromQuery] int hours = 24, CancellationToken cancellationToken = default)
        => Ok(await reportService.ServerUtilizationAsync(hours, cancellationToken));

    /// <summary>Exports the uptime report as CSV.</summary>
    [HttpGet("uptime/export")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportUptimeCsv(
        [FromQuery] int hours = 24, CancellationToken cancellationToken = default)
    {
        var rows = await reportService.UptimeAsync(hours, cancellationToken);
        var lines = new List<string> { "ServiceName,Environment,Status,UptimePercent,AvgResponseMs,CheckCount,DownCount" };
        lines.AddRange(rows.Select(r =>
            $"\"{r.ServiceName}\",{r.Environment},{r.CurrentStatus},{r.UptimePercent?.ToString("0.000") ?? ""},{r.AvgResponseMs?.ToString("0") ?? ""},{r.CheckCount},{r.DownCount}"));

        return File(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", lines)), "text/csv",
            $"opspulse-uptime-{hours}h-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
    }
}

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = Policies.CanManageOrganization)]
[Produces("application/json")]
public sealed class AuditLogsController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List(
        [FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
        => Ok(await auditService.ListAsync(query, cancellationToken));
}

[ApiController]
[Route("api/v1/api-keys")]
[Authorize(Policy = Policies.CanManageMonitoring)]
[Produces("application/json")]
public sealed class ApiKeysController(IApiKeyService apiKeyService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApiKeyDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> List(CancellationToken cancellationToken)
        => Ok(await apiKeyService.ListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ApiKeyCreatedDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiKeyCreatedDto>> Create(CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        var created = await apiKeyService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        await apiKeyService.RevokeAsync(id, cancellationToken);
        return NoContent();
    }
}

/// <summary>Agent ingest endpoints — authenticated with X-Api-Key, not JWT.</summary>
[ApiController]
[Route("api/v1/agents")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Produces("application/json")]
public sealed class AgentsController(IAgentIngestService agentIngestService) : ControllerBase
{
    [HttpPost("heartbeat")]
    [ProducesResponseType(typeof(AgentHeartbeatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentHeartbeatResponse>> Heartbeat(AgentHeartbeatRequest request, CancellationToken cancellationToken)
        => Ok(await agentIngestService.HeartbeatAsync(request, cancellationToken));

    [HttpPost("metrics")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Metrics(AgentMetricsBatchRequest request, CancellationToken cancellationToken)
    {
        await agentIngestService.SubmitMetricsAsync(request, cancellationToken);
        return Accepted();
    }
}

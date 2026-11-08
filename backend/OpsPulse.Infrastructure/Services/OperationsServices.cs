using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Application.Agents;
using OpsPulse.Application.Alerts;
using OpsPulse.Application.Common;
using OpsPulse.Application.Incidents;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Notifications;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Incidents;
using OpsPulse.Infrastructure.Monitoring;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class AlertRuleService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateAlertRuleRequest> createValidator,
    IValidator<UpdateAlertRuleRequest> updateValidator,
    IValidator<CreateAlertChannelRequest> channelValidator) : IAlertRuleService
{
    public async Task<IReadOnlyList<AlertRuleDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Scoped()
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new AlertRuleDto(
                r.Id, r.Name, r.Metric, r.Comparator, r.Threshold, r.DurationMinutes, r.CooldownMinutes,
                r.Severity, r.TargetServiceId,
                r.TargetServiceId == null ? null : db.MonitoredServices.Where(s => s.Id == r.TargetServiceId).Select(s => s.Name).FirstOrDefault(),
                r.IsEnabled, r.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AlertRuleDto> CreateAsync(CreateAlertRuleRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Your account has no organization context.");

        var rule = new AlertRule
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Metric = request.Metric,
            Comparator = request.Comparator,
            Threshold = request.Threshold,
            DurationMinutes = request.DurationMinutes,
            CooldownMinutes = request.CooldownMinutes,
            Severity = request.Severity,
            TargetServiceId = request.TargetServiceId
        };

        db.AlertRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);

        return await ProjectAsync(rule, cancellationToken);
    }

    public async Task<AlertRuleDto> UpdateAsync(Guid id, UpdateAlertRuleRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);

        var rule = await Scoped().FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Alert rule not found.");

        if (request.Name is not null) rule.Name = request.Name.Trim();
        if (request.Comparator is not null) rule.Comparator = request.Comparator.Value;
        if (request.Threshold is not null) rule.Threshold = request.Threshold.Value;
        if (request.DurationMinutes is not null) rule.DurationMinutes = request.DurationMinutes.Value;
        if (request.CooldownMinutes is not null) rule.CooldownMinutes = request.CooldownMinutes.Value;
        if (request.Severity is not null) rule.Severity = request.Severity.Value;
        if (request.TargetServiceId is not null) rule.TargetServiceId = request.TargetServiceId;
        if (request.IsEnabled is not null) rule.IsEnabled = request.IsEnabled.Value;

        await db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(rule, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await Scoped().FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Alert rule not found.");

        db.AlertRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertChannelDto>> ListChannelsAsync(CancellationToken cancellationToken = default)
    {
        return await ScopedChannels()
            .OrderBy(c => c.Name)
            .Select(c => new AlertChannelDto(c.Id, c.Name, c.Type, c.Target, c.IsEnabled, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<AlertChannelDto> CreateChannelAsync(CreateAlertChannelRequest request, CancellationToken cancellationToken = default)
    {
        await channelValidator.ValidateOrThrowAsync(request, cancellationToken);

        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Your account has no organization context.");

        var channel = new AlertChannel
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Target = request.Type == ChannelType.InApp ? null : request.Target?.Trim()
        };

        db.AlertChannels.Add(channel);
        await db.SaveChangesAsync(cancellationToken);

        return new AlertChannelDto(channel.Id, channel.Name, channel.Type, channel.Target, channel.IsEnabled, channel.CreatedAtUtc);
    }

    public async Task DeleteChannelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var channel = await ScopedChannels().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Alert channel not found.");

        db.AlertChannels.Remove(channel);
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<AlertRule> Scoped() =>
        currentUser.IsSuperAdmin ? db.AlertRules : db.AlertRules.Where(r => r.OrganizationId == currentUser.OrganizationId);

    private IQueryable<AlertChannel> ScopedChannels() =>
        currentUser.IsSuperAdmin ? db.AlertChannels : db.AlertChannels.Where(c => c.OrganizationId == currentUser.OrganizationId);

    private async Task<AlertRuleDto> ProjectAsync(AlertRule rule, CancellationToken cancellationToken)
    {
        var targetName = rule.TargetServiceId is { } targetId
            ? await db.MonitoredServices.Where(s => s.Id == targetId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new AlertRuleDto(rule.Id, rule.Name, rule.Metric, rule.Comparator, rule.Threshold,
            rule.DurationMinutes, rule.CooldownMinutes, rule.Severity, rule.TargetServiceId, targetName,
            rule.IsEnabled, rule.CreatedAtUtc);
    }
}

public sealed class IncidentService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IValidator<AddIncidentCommentRequest> commentValidator,
    IRealTimeNotifier notifier,
    IAuditLogger auditLogger) : IIncidentService
{
    public async Task<PagedResult<IncidentDto>> ListAsync(IncidentListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var incidents = db.Incidents.AsNoTracking();
        if (!currentUser.IsSuperAdmin)
        {
            incidents = incidents.Where(i => i.OrganizationId == currentUser.OrganizationId);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            incidents = incidents.Where(i => EF.Functions.ILike(i.Title, $"%{term}%") || EF.Functions.ILike(i.SourceName ?? "", $"%{term}%"));
        }
        if (query.Status is { } status)
        {
            incidents = incidents.Where(i => i.Status == status);
        }
        if (query.Severity is { } severity)
        {
            incidents = incidents.Where(i => i.Severity == severity);
        }

        var totalCount = await incidents.CountAsync(cancellationToken);
        var items = await incidents
            .OrderByDescending(i => i.LastEventAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<IncidentDto>.Create(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<IncidentDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await Scoped()
            .Include(i => i.Events.OrderByDescending(e => e.OccurredAtUtc))
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("Incident not found.");

        return new IncidentDetailsDto(
            ToDto(incident),
            incident.Events.Select(e => new IncidentEventDto(e.Id, e.Type, e.Message, e.ActorName, e.OccurredAtUtc)).ToList());
    }

    public async Task<IncidentDto> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await Scoped().Include(i => i.Events).FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("Incident not found.");

        if (incident.Status == IncidentStatus.Resolved)
        {
            throw new ConflictException("A resolved incident cannot be acknowledged.");
        }

        var now = DateTime.UtcNow;
        incident.Status = IncidentStatus.Acknowledged;
        incident.AcknowledgedAtUtc = now;
        incident.AcknowledgedByUserId = currentUser.UserId;
        incident.LastEventAtUtc = now;

        incident.Events.Add(new IncidentEvent
        {
            Type = IncidentEventType.StatusChanged,
            Message = $"Acknowledged by {currentUser.Email}",
            ActorUserId = currentUser.UserId,
            ActorName = currentUser.Email,
            OccurredAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await notifier.IncidentUpdatedAsync(incident.OrganizationId, IncidentManager.ToPayload(incident), cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "incident.acknowledged", incident.OrganizationId, currentUser.UserId, currentUser.Email,
            "Incident", incident.Id.ToString(), incident.Title), cancellationToken);
        return ToDto(incident);
    }

    public async Task<IncidentDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await Scoped().Include(i => i.Events).FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("Incident not found.");

        if (incident.Status == IncidentStatus.Resolved)
        {
            return ToDto(incident);
        }

        var now = DateTime.UtcNow;
        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedAtUtc = now;
        incident.ResolvedByUserId = currentUser.UserId;
        incident.LastEventAtUtc = now;

        incident.Events.Add(new IncidentEvent
        {
            Type = IncidentEventType.StatusChanged,
            Message = $"Resolved by {currentUser.Email}",
            ActorUserId = currentUser.UserId,
            ActorName = currentUser.Email,
            OccurredAtUtc = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await notifier.IncidentResolvedAsync(incident.OrganizationId, IncidentManager.ToPayload(incident), cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "incident.resolved", incident.OrganizationId, currentUser.UserId, currentUser.Email,
            "Incident", incident.Id.ToString(), incident.Title), cancellationToken);
        return ToDto(incident);
    }

    public async Task<IncidentDto> AddCommentAsync(Guid id, AddIncidentCommentRequest request, CancellationToken cancellationToken = default)
    {
        await commentValidator.ValidateOrThrowAsync(request, cancellationToken);

        var incident = await Scoped().Include(i => i.Events).FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("Incident not found.");

        var now = DateTime.UtcNow;
        incident.Events.Add(new IncidentEvent
        {
            Type = IncidentEventType.Comment,
            Message = request.Message,
            ActorUserId = currentUser.UserId,
            ActorName = currentUser.Email,
            OccurredAtUtc = now
        });
        incident.LastEventAtUtc = now;

        await db.SaveChangesAsync(cancellationToken);
        await notifier.IncidentUpdatedAsync(incident.OrganizationId, IncidentManager.ToPayload(incident), cancellationToken);
        return ToDto(incident);
    }

    private IQueryable<Incident> Scoped() =>
        currentUser.IsSuperAdmin ? db.Incidents : db.Incidents.Where(i => i.OrganizationId == currentUser.OrganizationId);

    private static IncidentDto ToDto(Incident incident)
    {
        var duration = (incident.ResolvedAtUtc ?? DateTime.UtcNow) - incident.StartedAtUtc;
        return new IncidentDto(incident.Id, incident.Title, incident.Description, incident.Severity,
            incident.Status, incident.SourceType, incident.SourceName, incident.StartedAtUtc,
            incident.AcknowledgedAtUtc, incident.ResolvedAtUtc, incident.LastEventAtUtc,
            (int)duration.TotalMinutes);
    }
}

public sealed class NotificationService(OpsPulseDbContext db, ICurrentUser currentUser) : INotificationService
{
    private IQueryable<Domain.Notifications.Notification> Scoped()
    {
        var baseQuery = currentUser.IsSuperAdmin
            ? db.Notifications
            : db.Notifications.Where(n => n.OrganizationId == currentUser.OrganizationId);
        return baseQuery.Where(n => n.UserId == currentUser.UserId || n.UserId == null);
    }

    public async Task<PagedResult<NotificationDto>> ListAsync(int page, int pageSize, bool? unreadOnly, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var notifications = Scoped();
        if (unreadOnly == true)
        {
            notifications = notifications.Where(n => !n.IsRead);
        }

        var totalCount = await notifications.CountAsync(cancellationToken);
        var items = await notifications
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type, n.Channel, n.IsRead, n.CreatedAtUtc, n.ReadAtUtc)).ToList();
        return PagedResult<NotificationDto>.Create(dtos, page, pageSize, totalCount);
    }

    public Task<UnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default)
        => Scoped().CountAsync(n => !n.IsRead, cancellationToken)
            .ContinueWith(t => new UnreadCountDto(t.Result), cancellationToken);

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await Scoped().FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var unread = await Scoped().Where(n => !n.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AgentIngestService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IRealTimeNotifier notifier,
    IAlertEvaluator alertEvaluator,
    Microsoft.Extensions.Options.IOptions<MonitoringOptions> monitoringOptions) : IAgentIngestService
{
    private readonly MonitoringOptions options = monitoringOptions.Value;
    public async Task<AgentHeartbeatResponse> HeartbeatAsync(AgentHeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("API key has no organization context.");
        var hostname = request.Hostname.Trim().ToLowerInvariant();

        var server = await db.Servers.FirstOrDefaultAsync(
            s => s.OrganizationId == organizationId && s.Hostname == hostname, cancellationToken);

        var wasOffline = server is null || server.Status != Domain.Servers.ServerStatus.Online;

        if (server is null)
        {
            server = new Domain.Servers.Server
            {
                OrganizationId = organizationId,
                Hostname = hostname,
                Name = string.IsNullOrWhiteSpace(request.Name) ? hostname : request.Name.Trim()
            };
            db.Servers.Add(server);
        }

        server.Name = string.IsNullOrWhiteSpace(request.Name) ? server.Name : request.Name.Trim();
        server.IpAddress = request.IpAddress;
        server.OsName = request.OsName;
        server.OsVersion = request.OsVersion;
        server.AgentVersion = request.AgentVersion;
        server.CpuCores = request.CpuCores;
        server.TotalMemoryMb = request.TotalMemoryMb;
        server.Status = Domain.Servers.ServerStatus.Online;
        server.LastHeartbeatAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        if (wasOffline)
        {
            await notifier.AgentConnectedAsync(organizationId,
                new AgentEventPayload(server.Id, server.Name, DateTime.UtcNow), cancellationToken);
        }

        return new AgentHeartbeatResponse(server.Id, options.AgentRecommendedIntervalSeconds);
    }

    public async Task SubmitMetricsAsync(AgentMetricsBatchRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("API key has no organization context.");

        var server = await db.Servers.FirstOrDefaultAsync(
                s => s.Id == request.ServerId && s.OrganizationId == organizationId, cancellationToken)
            ?? throw new NotFoundException("Server not found for this API key.");

        Domain.Servers.ServerMetric? latest = null;
        foreach (var sample in request.Metrics.Take(100))
        {
            latest = new Domain.Servers.ServerMetric
            {
                ServerId = server.Id,
                CollectedAtUtc = sample.CollectedAtUtc == default ? DateTime.UtcNow : sample.CollectedAtUtc,
                CpuUsagePercent = Math.Clamp(sample.CpuUsagePercent, 0, 100),
                MemoryUsedMb = sample.MemoryUsedMb,
                MemoryTotalMb = sample.MemoryTotalMb,
                DiskUsedPercent = sample.DiskUsedPercent is { } disk ? Math.Clamp(disk, 0, 100) : null,
                NetworkInMbps = sample.NetworkInMbps,
                NetworkOutMbps = sample.NetworkOutMbps
            };
            db.ServerMetrics.Add(latest);
        }

        if (latest is not null)
        {
            server.LastHeartbeatAtUtc = DateTime.UtcNow;
            server.Status = Domain.Servers.ServerStatus.Online;
            server.LastCpuUsagePercent = latest.CpuUsagePercent;
            server.LastMemoryUsagePercent = latest.MemoryTotalMb > 0
                ? Math.Round(latest.MemoryUsedMb * 100.0 / latest.MemoryTotalMb, 1)
                : null;
        }

        await db.SaveChangesAsync(cancellationToken);

        await notifier.MetricsUpdatedAsync(organizationId,
            new MetricsUpdatedPayload("server", server.Id, DateTime.UtcNow), cancellationToken);

        if (latest is not null)
        {
            await alertEvaluator.EvaluateMetricAsync(organizationId, AlertMetric.CpuUsage,
                latest.CpuUsagePercent, "server", server.Id, server.Name, cancellationToken);
            if (server.LastMemoryUsagePercent is { } memoryPercent)
            {
                await alertEvaluator.EvaluateMetricAsync(organizationId, AlertMetric.MemoryUsage,
                    memoryPercent, "server", server.Id, server.Name, cancellationToken);
            }
            if (latest.DiskUsedPercent is { } diskPercent)
            {
                await alertEvaluator.EvaluateMetricAsync(organizationId, AlertMetric.DiskUsage,
                    diskPercent, "server", server.Id, server.Name, cancellationToken);
            }
        }
    }
}

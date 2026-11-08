using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Application.Audit;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Reports;
using OpsPulse.Application.Security;
using OpsPulse.Domain.Incidents;
using OpsPulse.Domain.Servers;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class ReportService(OpsPulseDbContext db, ICurrentUser currentUser) : IReportService
{
    public async Task<IReadOnlyList<UptimeReportRow>> UptimeAsync(int hours, CancellationToken cancellationToken = default)
    {
        hours = Math.Clamp(hours, 1, 720);
        var from = DateTime.UtcNow.AddHours(-hours);

        var services = await ScopedServices()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var serviceIds = services.Select(s => s.Id).ToList();
        var stats = await db.ServiceChecks.AsNoTracking()
            .Where(c => serviceIds.Contains(c.ServiceId) && c.CheckedAtUtc >= from)
            .GroupBy(c => c.ServiceId)
            .Select(g => new
            {
                ServiceId = g.Key,
                Total = g.Count(),
                Success = g.Count(c => c.IsSuccess),
                AvgMs = g.Average(c => (double)c.ResponseTimeMs)
            })
            .ToListAsync(cancellationToken);

        var statsById = stats.ToDictionary(s => s.ServiceId);

        return services.Select(service =>
        {
            var hasStats = statsById.TryGetValue(service.Id, out var stat);
            return new UptimeReportRow(
                service.Id,
                service.Name,
                service.Environment.ToString(),
                service.CurrentStatus.ToString(),
                !hasStats ? null : Math.Round(stat!.Success * 100.0 / stat.Total, 3),
                !hasStats ? null : Math.Round(stat!.AvgMs, 1),
                hasStats ? stat!.Total : 0,
                hasStats ? stat!.Total - stat!.Success : 0);
        }).ToList();
    }

    public async Task<IncidentSummaryRow> IncidentsAsync(int hours, CancellationToken cancellationToken = default)
    {
        hours = Math.Clamp(hours, 1, 720);
        var from = DateTime.UtcNow.AddHours(-hours);

        var incidents = ScopedIncidents().Where(i => i.StartedAtUtc >= from);

        var counts = await incidents
            .GroupBy(i => 1)
            .Select(g => new
            {
                Open = g.Count(i => i.Status == IncidentStatus.Open),
                Acknowledged = g.Count(i => i.Status == IncidentStatus.Acknowledged),
                Resolved = g.Count(i => i.Status == IncidentStatus.Resolved),
                Informational = g.Count(i => i.Severity == IncidentSeverity.Informational),
                Warning = g.Count(i => i.Severity == IncidentSeverity.Warning),
                Critical = g.Count(i => i.Severity == IncidentSeverity.Critical)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var resolutions = await incidents
            .Where(i => i.ResolvedAtUtc != null)
            .Select(i => new { i.StartedAtUtc, i.ResolvedAtUtc })
            .ToListAsync(cancellationToken);

        var avgResolutionMinutes = resolutions.Count == 0
            ? (double?)null
            : Math.Round(resolutions.Average(i => (i.ResolvedAtUtc!.Value - i.StartedAtUtc).TotalMinutes), 1);

        return new IncidentSummaryRow(counts?.Open ?? 0, counts?.Acknowledged ?? 0, counts?.Resolved ?? 0,
            counts?.Informational ?? 0, counts?.Warning ?? 0, counts?.Critical ?? 0, avgResolutionMinutes);
    }

    public async Task<IReadOnlyList<ServerUtilizationRow>> ServerUtilizationAsync(int hours, CancellationToken cancellationToken = default)
    {
        hours = Math.Clamp(hours, 1, 720);
        var from = DateTime.UtcNow.AddHours(-hours);

        var servers = await ScopedServers().OrderBy(s => s.Name).ToListAsync(cancellationToken);
        var serverIds = servers.Select(s => s.Id).ToList();

        var stats = await db.ServerMetrics.AsNoTracking()
            .Where(m => serverIds.Contains(m.ServerId) && m.CollectedAtUtc >= from)
            .GroupBy(m => m.ServerId)
            .Select(g => new
            {
                ServerId = g.Key,
                AvgCpu = g.Average(m => (double)m.CpuUsagePercent),
                PeakCpu = g.Max(m => (double)m.CpuUsagePercent),
                AvgMem = g.Average(m => m.MemoryTotalMb > 0 ? (double)m.MemoryUsedMb * 100.0 / (double)m.MemoryTotalMb : 0.0),
                PeakMem = g.Max(m => m.MemoryTotalMb > 0 ? (double)m.MemoryUsedMb * 100.0 / (double)m.MemoryTotalMb : 0.0)
            })
            .ToListAsync(cancellationToken);

        var statsById = stats.ToDictionary(s => s.ServerId);

        return servers.Select(server =>
        {
            var has = statsById.TryGetValue(server.Id, out var stat);
            return new ServerUtilizationRow(
                server.Id,
                server.Name,
                server.Status.ToString(),
                has ? Math.Round(stat!.AvgCpu, 1) : null,
                has ? Math.Round(stat!.PeakCpu, 1) : null,
                has ? Math.Round(stat!.AvgMem, 1) : null,
                has ? Math.Round(stat!.PeakMem, 1) : null);
        }).ToList();
    }

    private IQueryable<Domain.Services.MonitoredService> ScopedServices() =>
        currentUser.IsSuperAdmin
            ? db.MonitoredServices.AsNoTracking()
            : db.MonitoredServices.AsNoTracking().Where(s => s.OrganizationId == currentUser.OrganizationId);

    private IQueryable<Incident> ScopedIncidents() =>
        currentUser.IsSuperAdmin
            ? db.Incidents.AsNoTracking()
            : db.Incidents.AsNoTracking().Where(i => i.OrganizationId == currentUser.OrganizationId);

    private IQueryable<Server> ScopedServers() =>
        currentUser.IsSuperAdmin
            ? db.Servers.AsNoTracking()
            : db.Servers.AsNoTracking().Where(s => s.OrganizationId == currentUser.OrganizationId);
}

public sealed class AuditService(OpsPulseDbContext db, ICurrentUser currentUser) : IAuditService
{
    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var logs = db.AuditLogs.AsNoTracking();
        if (!currentUser.IsSuperAdmin)
        {
            logs = logs.Where(l => l.OrganizationId == currentUser.OrganizationId);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            logs = logs.Where(l => EF.Functions.ILike(l.Action, $"%{term}%") ||
                                   EF.Functions.ILike(l.UserEmail ?? "", $"%{term}%") ||
                                   EF.Functions.ILike(l.Details ?? "", $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logs = logs.Where(l => l.Action == query.Action);
        }

        var totalCount = await logs.CountAsync(cancellationToken);
        var items = await logs
            .OrderByDescending(l => l.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto(l.Id, l.UserEmail, l.Action, l.EntityType, l.EntityId, l.Details, l.IpAddress, l.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return PagedResult<AuditLogDto>.Create(items, page, pageSize, totalCount);
    }
}

public sealed class ApiKeyService(OpsPulseDbContext db, ICurrentUser currentUser) : IApiKeyService
{
    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Scoped()
            .OrderByDescending(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyDto(k.Id, k.Name, k.KeyPrefix, k.IsActive, k.LastUsedAtUtc, k.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiKeyCreatedDto> CreateAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Your account has no organization context.");

        var rawKey = $"ops_{System.Buffers.Text.Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32))}";

        var apiKey = new Domain.Security.ApiKey
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            KeyHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawKey))),
            KeyPrefix = rawKey[..12],
            IsActive = true
        };

        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(cancellationToken);

        return new ApiKeyCreatedDto(
            new ApiKeyDto(apiKey.Id, apiKey.Name, apiKey.KeyPrefix, apiKey.IsActive, apiKey.LastUsedAtUtc, apiKey.CreatedAtUtc),
            rawKey);
    }

    public async Task RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var apiKey = await Scoped().FirstOrDefaultAsync(k => k.Id == id, cancellationToken)
            ?? throw new NotFoundException("API key not found.");

        apiKey.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Domain.Security.ApiKey> Scoped() =>
        currentUser.IsSuperAdmin
            ? db.ApiKeys
            : db.ApiKeys.Where(k => k.OrganizationId == currentUser.OrganizationId);
}

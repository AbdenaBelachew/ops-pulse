using Microsoft.EntityFrameworkCore;
using OpsPulse.Application.Common;
using OpsPulse.Application.InfrastructureMonitor;
using OpsPulse.Application.Interfaces;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

/// <summary>
/// Read access to the persisted platform log store (fed by the Serilog
/// PostgreSQL sink in both API and Worker processes).
/// </summary>
public sealed class LogService(OpsPulseDbContext db) : ILogService
{
    public async Task<PagedResult<PlatformLogDto>> ListAsync(PlatformLogQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 500);

        var logs = db.SystemLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            logs = logs.Where(l =>
                EF.Functions.ILike(l.Message, $"%{term}%") ||
                EF.Functions.ILike(l.Source ?? "", $"%{term}%") ||
                EF.Functions.ILike(l.Exception ?? "", $"%{term}%"));
        }

        if (!string.IsNullOrEmpty(query.Level))
        {
            logs = logs.Where(l => l.Level == query.Level);
        }

        if (!string.IsNullOrEmpty(query.Source))
        {
            logs = logs.Where(l => EF.Functions.ILike(l.Source ?? "", $"%{query.Source}%"));
        }

        var totalCount = await logs.CountAsync(cancellationToken);
        var items = await logs
            .OrderByDescending(l => l.TimestampUtc)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new PlatformLogDto(l.Id, l.TimestampUtc, l.Level, l.Message, l.Exception, l.Source, l.Process))
            .ToListAsync(cancellationToken);

        return PagedResult<PlatformLogDto>.Create(items, page, pageSize, totalCount);
    }
}

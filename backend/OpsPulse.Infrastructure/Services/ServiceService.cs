using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Audit;
using OpsPulse.Application.Common;
using OpsPulse.Application.Databases;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Servers;
using OpsPulse.Application.Services;
using OpsPulse.Domain.Databases;
using OpsPulse.Domain.Servers;
using OpsPulse.Domain.Services;
using OpsPulse.Infrastructure.Monitoring;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class ServiceService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateServiceRequest> createValidator,
    IValidator<UpdateServiceRequest> updateValidator,
    ServiceCheckProcessor checkProcessor,
    IAuditLogger auditLogger,
    ILogger<ServiceService> logger) : IServiceService
{
    public async Task<PagedResult<ServiceDto>> ListAsync(ServiceListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var services = db.MonitoredServices.AsNoTracking();
        if (!currentUser.IsSuperAdmin)
        {
            services = services.Where(s => s.OrganizationId == currentUser.OrganizationId);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            services = services.Where(s => EF.Functions.ILike(s.Name, $"%{term}%") || EF.Functions.ILike(s.Url, $"%{term}%"));
        }
        if (query.Environment is { } environment)
        {
            services = services.Where(s => s.Environment == environment);
        }
        if (query.Status is { } status)
        {
            services = services.Where(s => s.CurrentStatus == status);
        }

        var totalCount = await services.CountAsync(cancellationToken);
        var items = await services
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var uptime24h = await GetUptimeAsync(items.Select(s => s.Id).ToList(),
            DateTime.UtcNow.AddHours(-24), cancellationToken);

        var dtos = items.Select(s => ToDto(s,
            uptime24h.GetValueOrDefault(s.Id).Uptime,
            uptime24h.GetValueOrDefault(s.Id).AvgMs)).ToList();

        return PagedResult<ServiceDto>.Create(dtos, page, pageSize, totalCount);
    }

    public async Task<ServiceDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await Scoped().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service not found.");

        var uptime24h = await GetUptimeAsync([id], DateTime.UtcNow.AddHours(-24), cancellationToken);
        var uptime7d = await GetUptimeAsync([id], DateTime.UtcNow.AddDays(-7), cancellationToken);

        var (uptime24, avgMs24) = uptime24h.TryGetValue(id, out var entry24)
            ? (entry24.Uptime, entry24.AvgMs)
            : ((double?)null, (double?)null);
        var uptime7 = uptime7d.TryGetValue(id, out var entry7) ? entry7.Uptime : (double?)null;

        var checks = await db.ServiceChecks.AsNoTracking()
            .Where(c => c.ServiceId == id)
            .OrderByDescending(c => c.CheckedAtUtc)
            .Take(100)
            .Select(c => new ServiceCheckDto(c.CheckedAtUtc, c.IsSuccess, c.StatusCode, c.ResponseTimeMs, c.ErrorMessage))
            .ToListAsync(cancellationToken);

        return new ServiceDetailsDto(ToDto(service, uptime24, avgMs24), uptime24, uptime7, avgMs24, checks);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var organizationId = currentUser.OrganizationId
            ?? (currentUser.IsSuperAdmin
                ? throw new ValidationException("OrganizationId is required when a SuperAdmin creates a service.")
                : throw new ForbiddenException("Your account has no organization context."));

        if (await db.MonitoredServices.AnyAsync(s => s.OrganizationId == organizationId && s.Name == request.Name, cancellationToken))
        {
            throw new ConflictException("A service with this name already exists.");
        }

        var service = new MonitoredService
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            Description = request.Description,
            Environment = request.Environment,
            CheckIntervalSeconds = request.CheckIntervalSeconds,
            TimeoutSeconds = request.TimeoutSeconds,
            ExpectedStatusCodes = request.ExpectedStatusCodes
        };

        db.MonitoredServices.Add(service);
        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(new AuditEntry(
            "monitoring.service.created", organizationId, currentUser.UserId, currentUser.Email,
            "MonitoredService", service.Id.ToString(), service.Name), cancellationToken);

        return ToDto(service, null, null);
    }

    public async Task<ServiceDto> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);

        var service = await Scoped().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service not found.");

        if (request.Name is not null && request.Name != service.Name &&
            await db.MonitoredServices.AnyAsync(s => s.OrganizationId == service.OrganizationId && s.Name == request.Name && s.Id != id, cancellationToken))
        {
            throw new ConflictException("A service with this name already exists.");
        }

        if (request.Name is not null) service.Name = request.Name.Trim();
        if (request.Url is not null) service.Url = request.Url.Trim();
        if (request.Description is not null) service.Description = request.Description;
        if (request.Environment is not null) service.Environment = request.Environment.Value;
        if (request.CheckIntervalSeconds is not null) service.CheckIntervalSeconds = request.CheckIntervalSeconds.Value;
        if (request.TimeoutSeconds is not null) service.TimeoutSeconds = request.TimeoutSeconds.Value;
        if (request.ExpectedStatusCodes is not null) service.ExpectedStatusCodes = request.ExpectedStatusCodes;
        if (request.IsActive is not null)
        {
            service.IsActive = request.IsActive.Value;
            if (!service.IsActive)
            {
                service.CurrentStatus = ServiceStatus.Unknown;
                service.ConsecutiveFailures = 0;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "monitoring.service.updated", service.OrganizationId, currentUser.UserId, currentUser.Email,
            "MonitoredService", service.Id.ToString(), service.Name), cancellationToken);

        return ToDto(service, null, null);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await Scoped().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service not found.");

        db.MonitoredServices.Remove(service);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "monitoring.service.deleted", service.OrganizationId, currentUser.UserId, currentUser.Email,
            "MonitoredService", service.Id.ToString(), service.Name), cancellationToken);
    }

    public async Task<ServiceCheckDto> RunCheckAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var service = await db.MonitoredServices
            .FirstOrDefaultAsync(s => s.Id == id && (currentUser.IsSuperAdmin || s.OrganizationId == currentUser.OrganizationId), cancellationToken)
            ?? throw new NotFoundException("Service not found.");

        var check = await checkProcessor.ProcessAsync(service, cancellationToken);
        return new ServiceCheckDto(check.CheckedAtUtc, check.IsSuccess, check.StatusCode, check.ResponseTimeMs, check.ErrorMessage);
    }

    private IQueryable<MonitoredService> Scoped() =>
        currentUser.IsSuperAdmin ? db.MonitoredServices : db.MonitoredServices.Where(s => s.OrganizationId == currentUser.OrganizationId);

    private async Task<Dictionary<Guid, (double? Uptime, double? AvgMs)>> GetUptimeAsync(
        List<Guid> serviceIds, DateTime from, CancellationToken cancellationToken)
    {
        if (serviceIds.Count == 0)
        {
            return [];
        }

        var rows = await db.ServiceChecks.AsNoTracking()
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

        return rows.ToDictionary(
            r => r.ServiceId,
            r => (
                r.Total == 0 ? (double?)null : Math.Round(r.Success * 100.0 / r.Total, 2),
                (double?)Math.Round(r.AvgMs)));
    }

    private static ServiceDto ToDto(MonitoredService service, double? uptime24h, double? avgMs24h) =>
        new(service.Id, service.Name, service.Url, service.Description, service.Environment,
            service.CheckIntervalSeconds, service.TimeoutSeconds, service.ExpectedStatusCodes,
            service.IsActive, service.CurrentStatus, service.LastCheckedAtUtc, service.LastResponseTimeMs,
            service.ConsecutiveFailures, uptime24h, avgMs24h, service.CreatedAtUtc);
}

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Application.Audit;
using OpsPulse.Application.Common;
using OpsPulse.Application.Databases;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Servers;
using OpsPulse.Domain.Databases;
using OpsPulse.Domain.Servers;
using OpsPulse.Infrastructure.Monitoring;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class ServerService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IAuditLogger auditLogger) : IServerService
{
    public async Task<PagedResult<ServerDto>> ListAsync(ServerListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var servers = db.Servers.AsNoTracking();
        if (!currentUser.IsSuperAdmin)
        {
            servers = servers.Where(s => s.OrganizationId == currentUser.OrganizationId);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            servers = servers.Where(s => EF.Functions.ILike(s.Name, $"%{term}%") || EF.Functions.ILike(s.Hostname, $"%{term}%"));
        }
        if (query.Status is { } status)
        {
            servers = servers.Where(s => s.Status == status);
        }

        var totalCount = await servers.CountAsync(cancellationToken);
        var items = await servers
            .OrderByDescending(s => s.Status == ServerStatus.Online)
            .ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<ServerDto>.Create(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<ServerDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var server = await Scoped().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Server not found.");

        var metrics = await db.ServerMetrics.AsNoTracking()
            .Where(m => m.ServerId == id)
            .OrderByDescending(m => m.CollectedAtUtc)
            .Take(200)
            .Select(m => new ServerMetricDto(m.CollectedAtUtc, m.CpuUsagePercent, m.MemoryUsedMb, m.MemoryTotalMb, m.DiskUsedPercent, m.NetworkInMbps, m.NetworkOutMbps))
            .ToListAsync(cancellationToken);

        metrics.Reverse();
        return new ServerDetailsDto(ToDto(server), metrics);
    }

    public async Task<ServerDto> UpdateAsync(Guid id, UpdateServerRequest request, CancellationToken cancellationToken = default)
    {
        var server = await Scoped().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Server not found.");

        if (request.Name is not null)
        {
            server.Name = request.Name.Trim();
            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAsync(new AuditEntry(
                "monitoring.server.updated", server.OrganizationId, currentUser.UserId, currentUser.Email,
                "Server", server.Id.ToString(), server.Name), cancellationToken);
        }

        return ToDto(server);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var server = await Scoped().FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundException("Server not found.");

        db.Servers.Remove(server);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "monitoring.server.deleted", server.OrganizationId, currentUser.UserId, currentUser.Email,
            "Server", server.Id.ToString(), server.Name), cancellationToken);
    }

    private IQueryable<Server> Scoped() =>
        currentUser.IsSuperAdmin ? db.Servers : db.Servers.Where(s => s.OrganizationId == currentUser.OrganizationId);

    private static ServerDto ToDto(Server server) =>
        new(server.Id, server.Name, server.Hostname, server.IpAddress, server.OsName, server.OsVersion,
            server.AgentVersion, server.CpuCores, server.TotalMemoryMb, server.Status,
            server.LastHeartbeatAtUtc, server.LastCpuUsagePercent, server.LastMemoryUsagePercent, server.CreatedAtUtc);
}

public sealed class DatabaseService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateDatabaseConnectionRequest> createValidator,
    IValidator<UpdateDatabaseConnectionRequest> updateValidator,
    ICredentialProtector protector,
    IAuditLogger auditLogger) : IDatabaseService
{
    public async Task<PagedResult<DatabaseConnectionDto>> ListAsync(DatabaseListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var connections = db.DatabaseConnections.AsNoTracking();
        if (!currentUser.IsSuperAdmin)
        {
            connections = connections.Where(d => d.OrganizationId == currentUser.OrganizationId);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            connections = connections.Where(d => EF.Functions.ILike(d.Name, $"%{term}%") || EF.Functions.ILike(d.Host, $"%{term}%"));
        }
        if (query.DatabaseType is { } type)
        {
            connections = connections.Where(d => d.DatabaseType == type);
        }

        var totalCount = await connections.CountAsync(cancellationToken);
        var items = await connections
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<DatabaseConnectionDto>.Create(items.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<DatabaseDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await Scoped().FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("Database connection not found.");

        var metrics = await db.DatabaseMetrics.AsNoTracking()
            .Where(m => m.DatabaseConnectionId == id)
            .OrderByDescending(m => m.CollectedAtUtc)
            .Take(100)
            .Select(m => new DatabaseMetricDto(m.CollectedAtUtc, m.IsReachable, m.ActiveConnections, m.MaxConnections, m.DatabaseSizeMb, m.ResponseTimeMs, m.ErrorMessage))
            .ToListAsync(cancellationToken);

        metrics.Reverse();
        return new DatabaseDetailsDto(ToDto(connection), metrics);
    }

    public async Task<DatabaseConnectionDto> CreateAsync(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Your account has no organization context.");

        if (await db.DatabaseConnections.AnyAsync(d => d.OrganizationId == organizationId && d.Name == request.Name, cancellationToken))
        {
            throw new ConflictException("A database connection with this name already exists.");
        }

        var connection = new DatabaseConnection
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            DatabaseType = request.DatabaseType,
            Host = request.Host.Trim(),
            Port = request.Port,
            DatabaseName = request.DatabaseName.Trim(),
            Username = request.Username.Trim(),
            CredentialsCipher = protector.Protect(request.Password)
        };

        db.DatabaseConnections.Add(connection);
        await db.SaveChangesAsync(cancellationToken);

        await auditLogger.LogAsync(new AuditEntry(
            "monitoring.database.created", organizationId, currentUser.UserId, currentUser.Email,
            "DatabaseConnection", connection.Id.ToString(), connection.Name), cancellationToken);

        return ToDto(connection);
    }

    public async Task<DatabaseConnectionDto> UpdateAsync(Guid id, UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);

        var connection = await Scoped().FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("Database connection not found.");

        if (request.Name is not null) connection.Name = request.Name.Trim();
        if (request.Host is not null) connection.Host = request.Host.Trim();
        if (request.Port is not null) connection.Port = request.Port.Value;
        if (request.DatabaseName is not null) connection.DatabaseName = request.DatabaseName.Trim();
        if (request.Username is not null) connection.Username = request.Username.Trim();
        if (request.Password is not null) connection.CredentialsCipher = protector.Protect(request.Password);
        if (request.IsActive is not null) connection.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "monitoring.database.updated", connection.OrganizationId, currentUser.UserId, currentUser.Email,
            "DatabaseConnection", connection.Id.ToString(), connection.Name), cancellationToken);

        return ToDto(connection);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var connection = await Scoped().FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new NotFoundException("Database connection not found.");

        db.DatabaseConnections.Remove(connection);
        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "monitoring.database.deleted", connection.OrganizationId, currentUser.UserId, currentUser.Email,
            "DatabaseConnection", connection.Id.ToString(), connection.Name), cancellationToken);
    }

    private IQueryable<DatabaseConnection> Scoped() =>
        currentUser.IsSuperAdmin ? db.DatabaseConnections : db.DatabaseConnections.Where(d => d.OrganizationId == currentUser.OrganizationId);

    private static DatabaseConnectionDto ToDto(DatabaseConnection connection) =>
        new(connection.Id, connection.Name, connection.DatabaseType, connection.Host, connection.Port,
            connection.DatabaseName, connection.Username, connection.Health, connection.LastCheckedAtUtc,
            connection.IsActive, connection.CreatedAtUtc);
}

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Common;
using OpsPulse.Domain.Audit;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Audit;

/// <summary>
/// Writes audit entries on an isolated DbContext so auditing never commits
/// (or rolls back with) the caller's in-flight transaction.
/// </summary>
public sealed class AuditLogger(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditLogger> logger) : IAuditLogger
{
    public async Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OpsPulseDbContext>();

            db.AuditLogs.Add(new AuditLog
            {
                OrganizationId = entry.OrganizationId,
                UserId = entry.UserId,
                UserEmail = entry.UserEmail,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Details = entry.Details,
                IpAddress = entry.IpAddress ?? httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                OccurredAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to write audit log entry {Action}", entry.Action);
        }
    }
}

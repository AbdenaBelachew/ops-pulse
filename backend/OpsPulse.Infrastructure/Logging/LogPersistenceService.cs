using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpsPulse.Domain.Logging;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Logging;

/// <summary>
/// Drains the log sink queue into PostgreSQL every few seconds in batches.
/// Runs in both the API and Worker processes; failures fall back to the
/// console and never break the host.
/// </summary>
public sealed class LogPersistenceService(
    IServiceScopeFactory scopeFactory,
    ILogger<LogPersistenceService> consoleLogger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            var batch = PostgresLogSink.Drain(500);
            if (batch.Count == 0)
            {
                continue;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OpsPulseDbContext>();

                foreach (var record in batch)
                {
                    db.SystemLogs.Add(new SystemLog
                    {
                        TimestampUtc = record.TimestampUtc,
                        Level = record.Level,
                        Message = record.Message.Length > 4000 ? record.Message[..4000] : record.Message,
                        Exception = record.Exception is { Length: > 8000 } exception ? exception[..8000] : record.Exception,
                        Source = record.Source,
                        Process = record.Process
                    });
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                consoleLogger.LogError(exception, "Failed to persist {Count} log records", batch.Count);
            }
        }
    }
}

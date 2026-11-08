using System.Collections.Concurrent;
using System.Text.Json;
using Serilog.Core;
using Serilog.Events;

namespace OpsPulse.Infrastructure.Logging;

/// <summary>
/// In-process collection point for log events destined for PostgreSQL.
/// The sink only enqueues; <see cref="LogPersistenceService"/> drains in
/// batches so logging never blocks the request path.
/// </summary>
public sealed class PostgresLogSink : ILogEventSink
{
    private static readonly ConcurrentQueue<SystemLogRecord> Queue = new();
    private const int MaxQueue = 20_000;

    /// <summary>Record captured from a Serilog event.</summary>
    public sealed record SystemLogRecord(
        DateTime TimestampUtc,
        string Level,
        string Message,
        string? Exception,
        string? Source,
        string Process);

    public static PostgresLogSink Instance { get; } = new();

    private PostgresLogSink()
    {
    }

    public void Emit(LogEvent logEvent)
    {
        // Never persist the persistence pipeline itself (recursion guard).
        var source = logEvent.Properties.TryGetValue("SourceContext", out var sourceProperty)
            ? sourceProperty.ToString().Trim('"')
            : null;

        if (source is not null && source.StartsWith("OpsPulse.Infrastructure.Logging", StringComparison.Ordinal))
        {
            return;
        }

        if (Queue.Count >= MaxQueue)
        {
            Queue.TryDequeue(out _); // drop oldest under pressure
        }

        Queue.Enqueue(new SystemLogRecord(
            logEvent.Timestamp.UtcDateTime,
            logEvent.Level switch
            {
                LogEventLevel.Verbose => "Trace",
                LogEventLevel.Debug => "Debug",
                LogEventLevel.Information => "Information",
                LogEventLevel.Warning => "Warning",
                LogEventLevel.Error => "Error",
                _ => "Fatal"
            },
            logEvent.RenderMessage(),
            logEvent.Exception?.ToString(),
            source,
            Environment.MachineName + ":" + AppDomainName()));
    }

    /// <summary>Drains up to <paramref name="max"/> captured records.</summary>
    public static List<SystemLogRecord> Drain(int max = 500)
    {
        var batch = new List<SystemLogRecord>(Math.Min(max, Queue.Count));
        while (batch.Count < max && Queue.TryDequeue(out var record))
        {
            batch.Add(record);
        }

        return batch;
    }

    public static int PendingCount => Queue.Count;

    private static string AppDomainName()
    {
        try
        {
            return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}

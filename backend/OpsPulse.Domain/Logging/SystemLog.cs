using OpsPulse.Domain.Common;

namespace OpsPulse.Domain.Logging;

/// <summary>
/// A platform log record persisted by the Serilog→PostgreSQL sink. Gives
/// operators a first-class, searchable log store for locally deployed systems.
/// </summary>
public class SystemLog : BaseEntity
{
    public DateTime TimestampUtc { get; set; }

    /// <summary>Trace, Debug, Information, Warning, Error or Fatal.</summary>
    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }

    /// <summary>Serilog SourceContext, e.g. the emitting class.</summary>
    public string? Source { get; set; }

    /// <summary>Process that emitted the log: api, worker or agent.</summary>
    public string Process { get; set; } = string.Empty;
}

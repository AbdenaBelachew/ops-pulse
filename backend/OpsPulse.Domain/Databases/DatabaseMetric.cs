using OpsPulse.Domain.Common;

namespace OpsPulse.Domain.Databases;

/// <summary>
/// A point-in-time metric sample for a monitored database.
/// </summary>
public class DatabaseMetric : BaseEntity
{
    public Guid DatabaseConnectionId { get; set; }

    public DatabaseConnection DatabaseConnection { get; set; } = null!;

    public DateTime CollectedAtUtc { get; set; }

    public bool IsReachable { get; set; }

    public int? ActiveConnections { get; set; }

    public int? MaxConnections { get; set; }

    public long? DatabaseSizeMb { get; set; }

    public int? ResponseTimeMs { get; set; }

    public string? ErrorMessage { get; set; }
}

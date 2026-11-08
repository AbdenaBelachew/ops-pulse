using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Databases;

public enum DatabaseType
{
    PostgreSQL = 0,
    SqlServer = 1
}

public enum DatabaseHealth
{
    Unknown = 0,
    Healthy = 1,
    Degraded = 2,
    Unreachable = 3
}

/// <summary>
/// A registered database instance monitored by OpsPulse. Credentials are stored
/// encrypted, never in plain text.
/// </summary>
public class DatabaseConnection : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public DatabaseType DatabaseType { get; set; } = DatabaseType.PostgreSQL;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public string DatabaseName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    /// <summary>Encrypted password (AES-GCM, key from platform configuration).</summary>
    public string CredentialsCipher { get; set; } = string.Empty;

    public DatabaseHealth Health { get; set; } = DatabaseHealth.Unknown;

    public DateTime? LastCheckedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DatabaseMetric> Metrics { get; set; } = [];
}

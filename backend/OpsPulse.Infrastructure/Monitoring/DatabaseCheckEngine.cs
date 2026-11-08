using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpsPulse.Application.Common;
using OpsPulse.Domain.Databases;

namespace OpsPulse.Infrastructure.Monitoring;

public sealed record DatabaseCheckResult(
    bool IsReachable,
    int? ActiveConnections,
    int? MaxConnections,
    long? SizeMb,
    int ResponseTimeMs,
    string? ErrorMessage);

/// <summary>
/// Collects availability, connection and size metrics for registered
/// PostgreSQL and SQL Server databases. Credentials are decrypted per check
/// and never persisted in plain text.
/// </summary>
public sealed class DatabaseCheckEngine(ICredentialProtector protector, ILogger<DatabaseCheckEngine> logger)
{
    public async Task<DatabaseCheckResult> CheckAsync(Domain.Databases.DatabaseConnection connection, CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var password = protector.Unprotect(connection.CredentialsCipher);
            return connection.DatabaseType switch
            {
                DatabaseType.PostgreSQL => await CheckPostgreSqlAsync(connection, password, cancellationToken),
                DatabaseType.SqlServer => await CheckSqlServerAsync(connection, password, cancellationToken),
                _ => Failure(started, "Unsupported database type.")
            };
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Database check failed for {Name}", connection.Name);
            return Failure(started, exception.Message);
        }
    }

    private static async Task<DatabaseCheckResult> CheckPostgreSqlAsync(
        Domain.Databases.DatabaseConnection connection, string password, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = connection.Port,
            Username = connection.Username,
            Password = password,
            Database = connection.DatabaseName,
            Timeout = 5
        };

        var started = Stopwatch.GetTimestamp();
        await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
        await using var command = dataSource.CreateCommand(
            """
            SELECT
                (SELECT count(*)::int FROM pg_stat_activity WHERE datname = current_database()),
                (SELECT setting::int FROM pg_settings WHERE name = 'max_connections'),
                (pg_database_size(current_database()) / 1048576.0)::bigint
            """);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Failure(started, "No result returned.");
        }

        return new DatabaseCheckResult(
            true,
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt64(2),
            ElapsedMs(started),
            null);
    }

    private static async Task<DatabaseCheckResult> CheckSqlServerAsync(
        Domain.Databases.DatabaseConnection connection, string password, CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{connection.Host},{connection.Port}",
            InitialCatalog = connection.DatabaseName,
            UserID = connection.Username,
            Password = password,
            ConnectTimeout = 5,
            TrustServerCertificate = true
        };

        var started = Stopwatch.GetTimestamp();
        await using var sqlConnection = new SqlConnection(builder.ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT
                (SELECT count(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1),
                (SELECT CAST(value_in_use AS int) FROM sys.configurations WHERE name = 'user connections'),
                (SELECT CAST(SUM(size) * 8 / 1024 AS bigint) FROM sys.database_files WHERE state_desc = 'ONLINE')
            """, sqlConnection);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return Failure(started, "No result returned.");
        }

        return new DatabaseCheckResult(
            true,
            reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetInt64(2),
            ElapsedMs(started),
            null);
    }

    private static DatabaseCheckResult Failure(long started, string message) =>
        new(false, null, null, null, ElapsedMs(started), message);

    private static int ElapsedMs(long started) =>
        (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}

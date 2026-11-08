using Microsoft.Extensions.Options;

namespace OpsPulse.Infrastructure.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>Allow checks against private/loopback networks (development convenience, off by default).</summary>
    public bool AllowPrivateNetworks { get; set; }

    /// <summary>Successful checks slower than this are reported as Degraded.</summary>
    public int DegradedResponseTimeMs { get; set; } = 2000;

    /// <summary>Servers whose heartbeat is older than this are marked offline.</summary>
    public int AgentOfflineAfterSeconds { get; set; } = 90;

    /// <summary>Check history older than this is removed by the retention job.</summary>
    public int RetentionDays { get; set; } = 30;

    public int AgentRecommendedIntervalSeconds { get; set; } = 30;
}

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "opspulse@localhost";
    public string FromName { get; set; } = "OpsPulse";
}

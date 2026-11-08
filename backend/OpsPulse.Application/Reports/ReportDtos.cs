namespace OpsPulse.Application.Reports;

public sealed record UptimeReportRow(
    Guid ServiceId,
    string ServiceName,
    string Environment,
    string CurrentStatus,
    double? UptimePercent,
    double? AvgResponseMs,
    int CheckCount,
    int DownCount);

public sealed record IncidentSummaryRow(
    int Open,
    int Acknowledged,
    int Resolved,
    int Informational,
    int Warning,
    int Critical,
    double? AvgResolutionMinutes);

public sealed record ServerUtilizationRow(
    Guid ServerId,
    string ServerName,
    string Status,
    double? AvgCpuPercent,
    double? PeakCpuPercent,
    double? AvgMemoryPercent,
    double? PeakMemoryPercent);

namespace OpsPulse.Application.Audit;

public sealed record AuditLogDto(
    Guid Id,
    string? UserEmail,
    string Action,
    string? EntityType,
    string? EntityId,
    string? Details,
    string? IpAddress,
    DateTime OccurredAtUtc);

public sealed record AuditLogQuery(
    int Page = 1,
    int PageSize = 50,
    string? Search = null,
    string? Action = null);

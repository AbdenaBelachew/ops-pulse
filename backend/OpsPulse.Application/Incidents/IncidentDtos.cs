using FluentValidation;
using OpsPulse.Domain.Incidents;

namespace OpsPulse.Application.Incidents;

public sealed record IncidentDto(
    Guid Id,
    string Title,
    string? Description,
    IncidentSeverity Severity,
    IncidentStatus Status,
    string SourceType,
    string? SourceName,
    DateTime StartedAtUtc,
    DateTime? AcknowledgedAtUtc,
    DateTime? ResolvedAtUtc,
    DateTime LastEventAtUtc,
    int? DurationMinutes);

public sealed record IncidentEventDto(
    Guid Id,
    IncidentEventType Type,
    string Message,
    string? ActorName,
    DateTime OccurredAtUtc);

public sealed record IncidentDetailsDto(
    IncidentDto Incident,
    IReadOnlyList<IncidentEventDto> Events);

public sealed record IncidentListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    IncidentStatus? Status = null,
    IncidentSeverity? Severity = null);

public sealed record AddIncidentCommentRequest(string Message);

public sealed class AddIncidentCommentRequestValidator : AbstractValidator<AddIncidentCommentRequest>
{
    public AddIncidentCommentRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
    }
}

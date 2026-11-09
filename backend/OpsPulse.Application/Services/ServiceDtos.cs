using FluentValidation;
using OpsPulse.Domain.Services;

namespace OpsPulse.Application.Services;

public sealed record ServiceDto(
    Guid Id,
    string Name,
    string Url,
    string? Description,
    ServiceEnvironment Environment,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    string ExpectedStatusCodes,
    bool IsActive,
    ServiceStatus CurrentStatus,
    DateTime? LastCheckedAtUtc,
    int? LastResponseTimeMs,
    int ConsecutiveFailures,
    double? UptimePercent24h,
    double? AvgResponseMs24h,
    DateTime CreatedAtUtc);

public sealed record ServiceCheckDto(
    DateTime CheckedAtUtc,
    bool IsSuccess,
    int? StatusCode,
    int ResponseTimeMs,
    string? ErrorMessage);

public sealed record ServiceDetailsDto(
    ServiceDto Service,
    double? UptimePercent24h,
    double? UptimePercent7d,
    double? AvgResponseMs24h,
    IReadOnlyList<ServiceCheckDto> RecentChecks);

public sealed record CreateServiceRequest(
    string Name,
    string Url,
    string? Description,
    ServiceEnvironment Environment,
    int CheckIntervalSeconds,
    int TimeoutSeconds,
    string ExpectedStatusCodes);

public sealed record UpdateServiceRequest(
    string? Name = null,
    string? Url = null,
    string? Description = null,
    ServiceEnvironment? Environment = null,
    int? CheckIntervalSeconds = null,
    int? TimeoutSeconds = null,
    string? ExpectedStatusCodes = null,
    bool? IsActive = null);

public sealed record ServiceListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ServiceEnvironment? Environment = null,
    ServiceStatus? Status = null);

public sealed class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("URL must be an absolute http(s) address.");
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CheckIntervalSeconds).InclusiveBetween(10, 3600);
        RuleFor(x => x.TimeoutSeconds).InclusiveBetween(1, 60);
        RuleFor(x => x.ExpectedStatusCodes)
            .Matches(ExpectedStatusCodesFormat.Pattern)
            .WithMessage(ExpectedStatusCodesFormat.Message);
    }
}

public sealed class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200)
            .When(x => x.Name is not null);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2048)
            .Must(url => Uri.TryCreate(url!, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("URL must be an absolute http(s) address.")
            .When(x => x.Url is not null);
        RuleFor(x => x.CheckIntervalSeconds).InclusiveBetween(10, 3600)
            .When(x => x.CheckIntervalSeconds is not null);
        RuleFor(x => x.TimeoutSeconds).InclusiveBetween(1, 60)
            .When(x => x.TimeoutSeconds is not null);
        RuleFor(x => x.ExpectedStatusCodes)
            .Matches(ExpectedStatusCodesFormat.Pattern)
            .WithMessage(ExpectedStatusCodesFormat.Message)
            .When(x => x.ExpectedStatusCodes is not null);
    }
}

/// <summary>"200", "200,204", or ranges such as "200-399" (the checker follows redirects).</summary>
public static class ExpectedStatusCodesFormat
{
    public const string Pattern = @"^\d{3}(-\d{3})?(,\d{3}(-\d{3})?)*$";

    public const string Message =
        "Expected status codes must be 3-digit codes or ranges separated by commas, e.g. \"200\", \"200,204\" or \"200-399\".";
}

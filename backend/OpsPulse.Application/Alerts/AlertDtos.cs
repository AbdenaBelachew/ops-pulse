using FluentValidation;
using OpsPulse.Domain.Alerts;

namespace OpsPulse.Application.Alerts;

public sealed record AlertRuleDto(
    Guid Id,
    string Name,
    AlertMetric Metric,
    AlertComparator Comparator,
    double Threshold,
    int DurationMinutes,
    int CooldownMinutes,
    AlertSeverity Severity,
    Guid? TargetServiceId,
    string? TargetServiceName,
    bool IsEnabled,
    DateTime CreatedAtUtc);

public sealed record CreateAlertRuleRequest(
    string Name,
    AlertMetric Metric,
    AlertComparator Comparator,
    double Threshold,
    int DurationMinutes,
    int CooldownMinutes,
    AlertSeverity Severity,
    Guid? TargetServiceId);

public sealed record UpdateAlertRuleRequest(
    string? Name = null,
    AlertComparator? Comparator = null,
    double? Threshold = null,
    int? DurationMinutes = null,
    int? CooldownMinutes = null,
    AlertSeverity? Severity = null,
    Guid? TargetServiceId = null,
    bool? IsEnabled = null);

public sealed record AlertChannelDto(
    Guid Id,
    string Name,
    ChannelType Type,
    string? Target,
    bool IsEnabled,
    DateTime CreatedAtUtc);

public sealed record CreateAlertChannelRequest(string Name, ChannelType Type, string? Target);

public sealed class CreateAlertRuleRequestValidator : AbstractValidator<CreateAlertRuleRequest>
{
    public CreateAlertRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Threshold).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(0, 1440);
        RuleFor(x => x.CooldownMinutes).InclusiveBetween(0, 1440);
    }
}

public sealed class UpdateAlertRuleRequestValidator : AbstractValidator<UpdateAlertRuleRequest>
{
    public UpdateAlertRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Threshold).GreaterThan(0).When(x => x.Threshold is not null);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(0, 1440).When(x => x.DurationMinutes is not null);
        RuleFor(x => x.CooldownMinutes).InclusiveBetween(0, 1440).When(x => x.CooldownMinutes is not null);
    }
}

public sealed class CreateAlertChannelRequestValidator : AbstractValidator<CreateAlertChannelRequest>
{
    public CreateAlertChannelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Target)
            .NotEmpty()
            .When(x => x.Type == ChannelType.Email || x.Type == ChannelType.Webhook)
            .WithMessage("Email and webhook channels require a target.");
        RuleFor(x => x.Target)
            .EmailAddress()
            .When(x => x.Type == ChannelType.Email);
    }
}

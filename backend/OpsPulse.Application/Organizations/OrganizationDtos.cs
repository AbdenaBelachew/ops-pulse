using FluentValidation;

namespace OpsPulse.Application.Organizations;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    DateTime CreatedAtUtc);

public sealed record UpdateOrganizationRequest(string Name, string? Description);

public sealed class UpdateOrganizationRequestValidator : AbstractValidator<UpdateOrganizationRequest>
{
    public UpdateOrganizationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(3, 200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);
    }
}

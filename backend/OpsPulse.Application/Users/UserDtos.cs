using FluentValidation;
using OpsPulse.Domain.Common;

namespace OpsPulse.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? OrganizationId,
    string? OrganizationName,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateUserRequest(
    string Email,
    string FullName,
    string Password,
    string Role,
    Guid? OrganizationId = null);

public sealed record UpdateUserRequest(
    string? FullName = null,
    string? Role = null,
    bool? IsActive = null);

public sealed record UserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null,
    bool? IsActive = null);

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.Role)
            .Must(role => RoleNames.Assignable.Contains(role))
            .WithMessage("Role must be one of: " + string.Join(", ", RoleNames.Assignable));
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200)
            .When(x => x.FullName is not null);

        RuleFor(x => x.Role)
            .Must(role => role is null || RoleNames.Assignable.Contains(role))
            .WithMessage("Role must be one of: " + string.Join(", ", RoleNames.Assignable))
            .When(x => x.Role is not null);
    }
}

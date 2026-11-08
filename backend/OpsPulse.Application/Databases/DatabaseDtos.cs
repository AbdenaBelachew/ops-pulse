using FluentValidation;
using OpsPulse.Domain.Databases;

namespace OpsPulse.Application.Databases;

public sealed record DatabaseConnectionDto(
    Guid Id,
    string Name,
    DatabaseType DatabaseType,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    DatabaseHealth Health,
    DateTime? LastCheckedAtUtc,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record DatabaseMetricDto(
    DateTime CollectedAtUtc,
    bool IsReachable,
    int? ActiveConnections,
    int? MaxConnections,
    long? DatabaseSizeMb,
    int? ResponseTimeMs,
    string? ErrorMessage);

public sealed record DatabaseDetailsDto(
    DatabaseConnectionDto Connection,
    IReadOnlyList<DatabaseMetricDto> RecentMetrics);

public sealed record CreateDatabaseConnectionRequest(
    string Name,
    DatabaseType DatabaseType,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string Password);

public sealed record UpdateDatabaseConnectionRequest(
    string? Name = null,
    string? Host = null,
    int? Port = null,
    string? DatabaseName = null,
    string? Username = null,
    string? Password = null,
    bool? IsActive = null);

public sealed record DatabaseListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    DatabaseType? DatabaseType = null);

public sealed class CreateDatabaseConnectionRequestValidator : AbstractValidator<CreateDatabaseConnectionRequest>
{
    public CreateDatabaseConnectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Host).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535);
        RuleFor(x => x.DatabaseName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(500);
    }
}

public sealed class UpdateDatabaseConnectionRequestValidator : AbstractValidator<UpdateDatabaseConnectionRequest>
{
    public UpdateDatabaseConnectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).When(x => x.Name is not null);
        RuleFor(x => x.Host).NotEmpty().MaximumLength(255).When(x => x.Host is not null);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port is not null);
        RuleFor(x => x.DatabaseName).NotEmpty().MaximumLength(200).When(x => x.DatabaseName is not null);
        RuleFor(x => x.Username).NotEmpty().MaximumLength(200).When(x => x.Username is not null);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(500).When(x => x.Password is not null);
    }
}

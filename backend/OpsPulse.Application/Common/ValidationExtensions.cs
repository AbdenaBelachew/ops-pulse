using FluentValidation;

namespace OpsPulse.Application.Common;

public static class ValidationExtensions
{
    /// <summary>Validates an instance and throws <see cref="ValidationException"/> on failure.</summary>
    public static async Task ValidateOrThrowAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}

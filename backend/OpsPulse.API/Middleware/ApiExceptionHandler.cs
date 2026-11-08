using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Common;

namespace OpsPulse.API.Middleware;

/// <summary>
/// Maps application-level exceptions to RFC 9110 problem details responses.
/// </summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found.",
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.5"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Access denied.",
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.4"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict.",
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.10"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Authentication failed.",
                "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.2"),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = exception.Message,
                Type = type
            }
        });
    }
}

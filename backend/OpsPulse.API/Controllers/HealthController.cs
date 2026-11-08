using Microsoft.AspNetCore.Mvc;
using OpsPulse.API.Contracts;

namespace OpsPulse.API.Controllers;

[ApiController]
[Route("api/v1/health")]
[Produces("application/json")]
public sealed class HealthController(IHostEnvironment environment) : ControllerBase
{
    /// <summary>API metadata used by the frontend to verify connectivity.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiHealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var version = typeof(HealthController).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        return Ok(new ApiHealthResponse(
            Service: "OpsPulse.API",
            Version: version,
            Environment: environment.EnvironmentName,
            TimestampUtc: DateTime.UtcNow));
    }
}

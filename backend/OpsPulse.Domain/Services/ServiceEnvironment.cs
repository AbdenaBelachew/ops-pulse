namespace OpsPulse.Domain.Services;

/// <summary>
/// Deployment environment of a monitored service.
/// </summary>
public enum ServiceEnvironment
{
    Development = 0,
    Staging = 1,
    Production = 2
}

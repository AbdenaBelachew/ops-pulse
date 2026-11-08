namespace OpsPulse.Application.Authorization;

/// <summary>
/// Authorization policy names registered by the API host.
/// </summary>
public static class Policies
{
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageOrganization = "CanManageOrganization";
    public const string CanManagePlatform = "CanManagePlatform";

    /// <summary>Create/edit/delete monitored resources and configuration.</summary>
    public const string CanManageMonitoring = "CanManageMonitoring";

    /// <summary>Includes operators: trigger manual checks, acknowledge incidents.</summary>
    public const string CanOperateMonitoring = "CanOperateMonitoring";

    public const string CanManageIncidents = "CanManageIncidents";
}

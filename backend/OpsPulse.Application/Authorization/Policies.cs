namespace OpsPulse.Application.Authorization;

/// <summary>
/// Authorization policy names registered by the API host.
/// </summary>
public static class Policies
{
    public const string CanManageUsers = "CanManageUsers";
    public const string CanManageOrganization = "CanManageOrganization";
    public const string CanManagePlatform = "CanManagePlatform";
}

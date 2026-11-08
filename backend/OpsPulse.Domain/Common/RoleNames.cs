namespace OpsPulse.Domain.Common;

/// <summary>
/// Platform role names. Every user holds exactly one role.
/// </summary>
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string OrganizationAdmin = "OrganizationAdmin";
    public const string DevOpsEngineer = "DevOpsEngineer";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";

    public static readonly string[] All =
    [
        SuperAdmin,
        OrganizationAdmin,
        DevOpsEngineer,
        Operator,
        Viewer
    ];

    /// <summary>Roles that can be assigned when creating or updating users.</summary>
    public static readonly string[] Assignable = All;
}

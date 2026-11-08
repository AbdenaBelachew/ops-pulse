namespace OpsPulse.Application.Common;

/// <summary>
/// Abstraction over the authenticated request principal, resolved by the API host
/// from validated JWT claims.
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    string Email { get; }

    Guid? OrganizationId { get; }

    string Role { get; }

    bool IsSuperAdmin { get; }
}

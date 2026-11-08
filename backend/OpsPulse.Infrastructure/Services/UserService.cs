using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Users;
using OpsPulse.Domain.Common;
using OpsPulse.Infrastructure.Identity;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class UserService(
    OpsPulseDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator) : IUserService
{
    public async Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var users = db.Users.AsNoTracking();

        // Organization-level tenant isolation; SuperAdmin sees the whole platform.
        if (!currentUser.IsSuperAdmin)
        {
            users = users.Where(u => u.OrganizationId == currentUser.OrganizationId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            users = users.Where(u =>
                EF.Functions.ILike(u.Email!, $"%{term}%") ||
                EF.Functions.ILike(u.FullName, $"%{term}%"));
        }

        if (query.IsActive is { } isActive)
        {
            users = users.Where(u => u.IsActive == isActive);
        }

        if (!string.IsNullOrEmpty(query.Role))
        {
            var roleId = await db.Roles
                .Where(r => r.Name == query.Role)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            users = users.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
        }

        var totalCount = await users.CountAsync(cancellationToken);

        var items = await users
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FullName,
                u.OrganizationId,
                u.IsActive,
                u.CreatedAtUtc,
                OrganizationName = u.OrganizationId == null
                    ? null
                    : db.Organizations.Where(o => o.Id == u.OrganizationId).Select(o => o.Name).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var roleMap = await GetRoleMapAsync(items.Select(i => i.Id).ToList(), cancellationToken);

        var dtos = items
            .Select(i => new UserDto(
                i.Id,
                i.Email ?? string.Empty,
                i.FullName,
                roleMap.GetValueOrDefault(i.Id, string.Empty),
                i.OrganizationId,
                i.OrganizationName,
                i.IsActive,
                i.CreatedAtUtc))
            .ToList();

        return PagedResult<UserDto>.Create(dtos, page, pageSize, totalCount);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        if (request.Role == RoleNames.SuperAdmin && !currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("Only a SuperAdmin can create SuperAdmin accounts.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        Guid? organizationId;
        if (request.Role == RoleNames.SuperAdmin)
        {
            organizationId = null;
        }
        else if (currentUser.IsSuperAdmin)
        {
            organizationId = request.OrganizationId
                ?? throw new ValidationException("OrganizationId is required when a SuperAdmin creates an organization-scoped user.");

            if (!await db.Organizations.AnyAsync(o => o.Id == organizationId, cancellationToken))
            {
                throw new NotFoundException("Organization not found.");
            }
        }
        else
        {
            organizationId = currentUser.OrganizationId
                ?? throw new ForbiddenException("Your account has no organization context.");
        }

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            OrganizationId = organizationId,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new ValidationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, request.Role);

        string? organizationName = null;
        if (user.OrganizationId is { } createdOrganizationId)
        {
            organizationName = await db.Organizations
                .Where(o => o.Id == createdOrganizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new UserDto(
            user.Id,
            user.Email!,
            user.FullName,
            request.Role,
            user.OrganizationId,
            organizationName,
            user.IsActive,
            user.CreatedAtUtc);
    }

    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        var targetRoles = await userManager.GetRolesAsync(user);
        if (targetRoles.Contains(RoleNames.SuperAdmin) && !currentUser.IsSuperAdmin)
        {
            throw new ForbiddenException("Only a SuperAdmin can modify SuperAdmin accounts.");
        }

        if (request.FullName is not null)
        {
            user.FullName = request.FullName.Trim();
        }

        if (request.Role is { } newRole && newRole != targetRoles.FirstOrDefault())
        {
            if (userId == currentUser.UserId)
            {
                throw new ForbiddenException("You cannot change your own role.");
            }

            if (newRole == RoleNames.SuperAdmin && !currentUser.IsSuperAdmin)
            {
                throw new ForbiddenException("Only a SuperAdmin can grant the SuperAdmin role.");
            }

            await userManager.RemoveFromRolesAsync(user, targetRoles);
            await userManager.AddToRoleAsync(user, newRole);
        }

        if (request.IsActive is { } isActive)
        {
            if (!isActive && userId == currentUser.UserId)
            {
                throw new ForbiddenException("You cannot deactivate your own account.");
            }

            user.IsActive = isActive;

            if (!isActive)
            {
                // Sign the deactivated user out everywhere.
                var activeTokens = await db.RefreshTokens
                    .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);

                foreach (var token in activeTokens)
                {
                    token.RevokedAtUtc = DateTime.UtcNow;
                }
            }
        }

        await userManager.UpdateAsync(user);

        string? organizationName = null;
        if (user.OrganizationId is { } organizationId)
        {
            organizationName = await db.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var role = (request.Role ?? targetRoles.FirstOrDefault()) ?? string.Empty;

        return new UserDto(
            user.Id,
            user.Email!,
            user.FullName,
            role,
            user.OrganizationId,
            organizationName,
            user.IsActive,
            user.CreatedAtUtc);
    }

    private async Task<Dictionary<Guid, string>> GetRoleMapAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken)
    {
        return await db.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (userRole, role) => new { userRole.UserId, role.Name })
            .ToDictionaryAsync(x => x.UserId, x => x.Name ?? string.Empty, cancellationToken);
    }
}

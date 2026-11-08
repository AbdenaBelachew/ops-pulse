using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Auth;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Users;
using OpsPulse.Domain.Common;
using OpsPulse.Domain.Identity;
using OpsPulse.Infrastructure.Identity;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class IdentityService(
    OpsPulseDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    JwtTokenService tokenService,
    ICurrentUser currentUser,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    ILogger<IdentityService> logger) : IIdentityService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await registerValidator.ValidateOrThrowAsync(request, cancellationToken);

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var organization = new Domain.Organizations.Organization
        {
            Name = request.OrganizationName.Trim(),
            Slug = await GenerateUniqueSlugAsync(request.OrganizationName, cancellationToken)
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            OrganizationId = organization.Id,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new ValidationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, RoleNames.OrganizationAdmin);
        logger.LogInformation(
            "Organization {OrganizationName} registered with admin {Email}",
            organization.Name, normalizedEmail);

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateOrThrowAsync(request, cancellationToken);

        var user = await userManager.FindByEmailAsync(NormalizeEmail(request.Email));
        if (user is null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!signInResult.Succeeded)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        await refreshValidator.ValidateOrThrowAsync(request, cancellationToken);

        var tokenHash = JwtTokenService.HashToken(request.RefreshToken);
        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedException("Invalid refresh token.");
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            // Reuse of a rotated token indicates possible theft — revoke the whole chain.
            logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; revoking all active tokens",
                storedToken.UserId);
            await RevokeAllActiveTokensAsync(storedToken.UserId, cancellationToken);
            throw new UnauthorizedException("Refresh token has been revoked.");
        }

        if (storedToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException("Account is inactive.");
        }

        var (newRefreshToken, newRefreshExpires) = tokenService.CreateRefreshToken();
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = JwtTokenService.HashToken(newRefreshToken);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = JwtTokenService.HashToken(newRefreshToken),
            ExpiresAtUtc = newRefreshExpires
        });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAuthResponseAsync(user, newRefreshToken, newRefreshExpires, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = JwtTokenService.HashToken(request.RefreshToken);
        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is { RevokedAtUtc: null } && storedToken.UserId == currentUser.UserId)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<UserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new NotFoundException("User not found.");

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        var (role, organizationName) = await GetUserContextAsync(user, cancellationToken);
        return ToDto(user, role, organizationName);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var (refreshToken, refreshExpires) = tokenService.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = JwtTokenService.HashToken(refreshToken),
            ExpiresAtUtc = refreshExpires
        });

        var (role, organizationName) = await GetUserContextAsync(user, cancellationToken);

        var (accessToken, accessExpires) = tokenService.CreateAccessToken(
            user.Id, user.Email!, user.FullName, user.OrganizationId, role);

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, accessExpires, ToDto(user, role, organizationName));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        ApplicationUser user,
        string refreshToken,
        DateTime refreshExpires,
        CancellationToken cancellationToken)
    {
        var (role, organizationName) = await GetUserContextAsync(user, cancellationToken);

        var (accessToken, accessExpires) = tokenService.CreateAccessToken(
            user.Id, user.Email!, user.FullName, user.OrganizationId, role);

        return new AuthResponse(accessToken, refreshToken, accessExpires, ToDto(user, role, organizationName));
    }

    private async Task<(string Role, string? OrganizationName)> GetUserContextAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);

        string? organizationName = null;
        if (user.OrganizationId is { } organizationId)
        {
            organizationName = await db.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return (roles.FirstOrDefault() ?? string.Empty, organizationName);
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static UserDto ToDto(ApplicationUser user, string role, string? organizationName) =>
        new(
            user.Id,
            user.Email!,
            user.FullName,
            role,
            user.OrganizationId,
            organizationName,
            user.IsActive,
            user.CreatedAtUtc);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        var counter = 1;

        while (await db.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{++counter}";
        }

        return slug;
    }

    private static string Slugify(string name)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "organization" : slug;
    }
}

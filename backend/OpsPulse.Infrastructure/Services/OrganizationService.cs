using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Organizations;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class OrganizationService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IValidator<UpdateOrganizationRequest> updateValidator) : IOrganizationService
{
    public async Task<PagedResult<OrganizationDto>> ListAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var organizations = db.Organizations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            organizations = organizations.Where(o =>
                EF.Functions.ILike(o.Name, $"%{term}%") ||
                EF.Functions.ILike(o.Slug, $"%{term}%"));
        }

        var totalCount = await organizations.CountAsync(cancellationToken);

        var items = await organizations
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationDto(o.Id, o.Name, o.Slug, o.Description, o.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return PagedResult<OrganizationDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<OrganizationDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var organizationId = currentUser.OrganizationId
            ?? throw new NotFoundException("Your account is not linked to an organization.");

        var organization = await db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
            ?? throw new NotFoundException("Organization not found.");

        return ToDto(organization);
    }

    public async Task<OrganizationDto> UpdateCurrentAsync(
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateOrThrowAsync(request, cancellationToken);

        var organizationId = currentUser.OrganizationId
            ?? throw new NotFoundException("Your account is not linked to an organization.");

        var organization = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
            ?? throw new NotFoundException("Organization not found.");

        organization.Name = request.Name.Trim();
        organization.Description = request.Description;

        await db.SaveChangesAsync(cancellationToken);

        return ToDto(organization);
    }

    private static OrganizationDto ToDto(Domain.Organizations.Organization organization) =>
        new(organization.Id, organization.Name, organization.Slug, organization.Description, organization.CreatedAtUtc);
}

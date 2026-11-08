using OpsPulse.Application.Common;
using OpsPulse.Application.Organizations;

namespace OpsPulse.Application.Interfaces;

public interface IOrganizationService
{
    Task<PagedResult<OrganizationDto>> ListAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<OrganizationDto> UpdateCurrentAsync(UpdateOrganizationRequest request, CancellationToken cancellationToken = default);
}

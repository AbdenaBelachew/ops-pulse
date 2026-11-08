using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Organizations;

namespace OpsPulse.API.Controllers;

[ApiController]
[Route("api/v1/organizations")]
[Authorize]
[Produces("application/json")]
public sealed class OrganizationsController(IOrganizationService organizationService) : ControllerBase
{
    /// <summary>Lists organizations (platform administrators only).</summary>
    [HttpGet]
    [Authorize(Policy = Policies.CanManagePlatform)]
    [ProducesResponseType(typeof(PagedResult<OrganizationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrganizationDto>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => Ok(await organizationService.ListAsync(page, pageSize, search, cancellationToken));

    /// <summary>Returns the caller's organization.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDto>> GetCurrent(CancellationToken cancellationToken)
        => Ok(await organizationService.GetCurrentAsync(cancellationToken));

    /// <summary>Updates the caller's organization profile.</summary>
    [HttpPut("current")]
    [Authorize(Policy = Policies.CanManageOrganization)]
    [ProducesResponseType(typeof(OrganizationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrganizationDto>> UpdateCurrent(
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
        => Ok(await organizationService.UpdateCurrentAsync(request, cancellationToken));
}

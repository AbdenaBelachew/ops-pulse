using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Users;

namespace OpsPulse.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>Lists users. Organization admins see their own organization; SuperAdmin sees all.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> List(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken)
        => Ok(await userService.ListAsync(query, cancellationToken));

    /// <summary>Creates a user in the caller's organization (SuperAdmin may target any organization).</summary>
    [HttpPost]
    [Authorize(Policy = Policies.CanManageUsers)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Updates full name, role and/or active state of a user.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageUsers)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
        => Ok(await userService.UpdateAsync(id, request, cancellationToken));
}

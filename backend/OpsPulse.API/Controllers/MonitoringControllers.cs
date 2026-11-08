using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.Common;
using OpsPulse.Application.Databases;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Servers;
using OpsPulse.Application.Services;
using OpsPulse.Domain.Services;

namespace OpsPulse.API.Controllers;

[ApiController]
[Route("api/v1/services")]
[Authorize]
[Produces("application/json")]
public sealed class ServicesController(IServiceService serviceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServiceDto>>> List(
        [FromQuery] ServiceListQuery query, CancellationToken cancellationToken)
        => Ok(await serviceService.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServiceDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await serviceService.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceDto>> Create(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await serviceService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, service);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceDto>> Update(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken)
        => Ok(await serviceService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await serviceService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Triggers an immediate HTTP check outside the regular schedule.</summary>
    [HttpPost("{id:guid}/check")]
    [Authorize(Policy = Policies.CanOperateMonitoring)]
    [ProducesResponseType(typeof(ServiceCheckDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServiceCheckDto>> RunCheck(Guid id, CancellationToken cancellationToken)
        => Ok(await serviceService.RunCheckAsync(id, cancellationToken));
}

[ApiController]
[Route("api/v1/servers")]
[Authorize]
[Produces("application/json")]
public sealed class ServersController(IServerService serverService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ServerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ServerDto>>> List(
        [FromQuery] ServerListQuery query, CancellationToken cancellationToken)
        => Ok(await serverService.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServerDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await serverService.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(ServerDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServerDto>> Update(Guid id, UpdateServerRequest request, CancellationToken cancellationToken)
        => Ok(await serverService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await serverService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/databases")]
[Authorize]
[Produces("application/json")]
public sealed class DatabasesController(IDatabaseService databaseService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DatabaseConnectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DatabaseConnectionDto>>> List(
        [FromQuery] DatabaseListQuery query, CancellationToken cancellationToken)
        => Ok(await databaseService.ListAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DatabaseDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await databaseService.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(DatabaseConnectionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DatabaseConnectionDto>> Create(CreateDatabaseConnectionRequest request, CancellationToken cancellationToken)
    {
        var connection = await databaseService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, connection);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(DatabaseConnectionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseConnectionDto>> Update(Guid id, UpdateDatabaseConnectionRequest request, CancellationToken cancellationToken)
        => Ok(await databaseService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await databaseService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

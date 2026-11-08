using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.InfrastructureMonitor;
using OpsPulse.Application.Interfaces;

namespace OpsPulse.API.Controllers;

[ApiController]
[Route("api/v1/docker")]
[Authorize]
[Produces("application/json")]
public sealed class DockerController(IDockerService dockerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DockerHostDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DockerHostDto>>> List(CancellationToken cancellationToken)
        => Ok(await dockerService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DockerHostDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DockerHostDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await dockerService.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(DockerHostDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DockerHostDto>> Create(CreateDockerHostRequest request, CancellationToken cancellationToken)
    {
        var host = await dockerService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, host);
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(DockerHostDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DockerHostDto>> Sync(Guid id, CancellationToken cancellationToken)
        => Ok(await dockerService.SyncAsync(id, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await dockerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

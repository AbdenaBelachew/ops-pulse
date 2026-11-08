using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.InfrastructureMonitor;
using OpsPulse.Application.Interfaces;

namespace OpsPulse.API.Controllers;

[ApiController]
[Route("api/v1/kubernetes")]
[Authorize]
[Produces("application/json")]
public sealed class KubernetesController(IKubernetesService kubernetesService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<KubernetesClusterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KubernetesClusterDto>>> List(CancellationToken cancellationToken)
        => Ok(await kubernetesService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KubernetesClusterDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KubernetesClusterDetailsDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await kubernetesService.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(KubernetesClusterDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<KubernetesClusterDto>> Create(CreateKubernetesClusterRequest request, CancellationToken cancellationToken)
    {
        var cluster = await kubernetesService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, cluster);
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(KubernetesClusterDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KubernetesClusterDto>> Sync(Guid id, CancellationToken cancellationToken)
        => Ok(await kubernetesService.SyncAsync(id, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await kubernetesService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.Common;
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

    /// <summary>Streams the last lines of a pod's log output.</summary>
    [HttpGet("{id:guid}/pods/logs")]
    [Authorize(Policy = Policies.CanOperateMonitoring)]
    [ProducesResponseType(typeof(PodLogsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PodLogsDto>> PodLogs(
        Guid id,
        [FromQuery] string namespaceParameter,
        [FromQuery] string pod,
        [FromQuery] int lines = 200,
        [FromQuery] bool previous = false,
        CancellationToken cancellationToken = default)
        => Ok(await kubernetesService.GetPodLogsAsync(id, namespaceParameter, pod, lines, previous, cancellationToken));

    /// <summary>Restarts a pod by deleting it (its controller recreates it).</summary>
    [HttpPost("{id:guid}/pods/{namespaceParameter}/{pod}/restart")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RestartPod(Guid id, string namespaceParameter, string pod, CancellationToken cancellationToken)
    {
        await kubernetesService.RestartPodAsync(id, namespaceParameter, pod, cancellationToken);
        return NoContent();
    }

    /// <summary>Scales a deployment to the requested replica count.</summary>
    [HttpPost("{id:guid}/deployments/{namespaceParameter}/{deploymentName}/scale")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(typeof(KubernetesDeploymentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<KubernetesDeploymentDto>> ScaleDeployment(
        Guid id, string namespaceParameter, string deploymentName, ScaleDeploymentRequest request, CancellationToken cancellationToken)
        => Ok(await kubernetesService.ScaleDeploymentAsync(id, namespaceParameter, deploymentName, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanManageMonitoring)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await kubernetesService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

/// <summary>Platform log store — the Serilog→PostgreSQL pipeline output.</summary>
[ApiController]
[Route("api/v1/logs")]
[Authorize(Policy = Policies.CanManageMonitoring)]
[Produces("application/json")]
public sealed class LogsController(ILogService logService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PlatformLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PlatformLogDto>>> List(
        [FromQuery] PlatformLogQuery query, CancellationToken cancellationToken)
        => Ok(await logService.ListAsync(query, cancellationToken));
}

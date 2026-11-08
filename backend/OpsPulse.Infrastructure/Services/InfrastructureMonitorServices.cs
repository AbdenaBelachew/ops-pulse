using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Application.Common;
using OpsPulse.Application.InfrastructureMonitor;
using OpsPulse.Application.Interfaces;
using OpsPulse.Domain.Containers;
using OpsPulse.Domain.Kubernetes;
using OpsPulse.Infrastructure.Monitoring;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Services;

public sealed class KubernetesService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateKubernetesClusterRequest> createValidator,
    ICredentialProtector credentialProtector,
    KubernetesSyncEngine syncEngine,
    IAuditLogger auditLogger) : IKubernetesService
{
    public async Task<IReadOnlyList<KubernetesClusterDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var clusters = await Scoped().OrderBy(c => c.Name).ToListAsync(cancellationToken);
        return clusters.Select(Project).ToList();
    }

    public async Task<KubernetesClusterDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cluster = await Scoped().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cluster not found.");

        var nodes = await db.KubernetesNodes.AsNoTracking()
            .Where(n => n.ClusterId == id)
            .OrderBy(n => n.Name)
            .Select(n => new KubernetesNodeDto(n.Id, n.Name, n.Status, n.Roles, n.KubernetesVersion, n.CpuCapacityCores, n.MemoryCapacityMb, n.SyncedAtUtc))
            .ToListAsync(cancellationToken);

        var pods = await db.KubernetesPods.AsNoTracking()
            .Where(p => p.ClusterId == id)
            .OrderBy(p => p.Namespace).ThenBy(p => p.Name)
            .Select(p => new KubernetesPodDto(p.Id, p.Namespace, p.Name, p.Status, p.RestartCount, p.NodeName, p.IsFailed, p.StartedAtUtc, p.SyncedAtUtc))
            .ToListAsync(cancellationToken);

        var deployments = await db.KubernetesDeployments.AsNoTracking()
            .Where(d => d.ClusterId == id)
            .OrderBy(d => d.Namespace).ThenBy(d => d.Name)
            .Select(d => new KubernetesDeploymentDto(d.Id, d.Namespace, d.Name, d.ReplicasDesired, d.ReplicasReady, d.Image, d.SyncedAtUtc))
            .ToListAsync(cancellationToken);

        return new KubernetesClusterDetailsDto(Project(cluster), nodes, pods, deployments);
    }

    public async Task<KubernetesClusterDto> CreateAsync(CreateKubernetesClusterRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Your account has no organization context.");

        if (await db.KubernetesClusters.AnyAsync(c => c.OrganizationId == organizationId && c.Name == request.Name, cancellationToken))
        {
            throw new ConflictException("A cluster with this name already exists.");
        }

        var cluster = new KubernetesCluster
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            KubeConfigCipher = string.IsNullOrWhiteSpace(request.KubeConfigYaml)
                ? null
                : credentialProtector.Protect(request.KubeConfigYaml)
        };

        db.KubernetesClusters.Add(cluster);
        await db.SaveChangesAsync(cancellationToken);
        await syncEngine.SyncAsync(cluster, cancellationToken);

        return Project(cluster);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cluster = await Scoped().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cluster not found.");

        db.KubernetesClusters.Remove(cluster);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<KubernetesClusterDto> SyncAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cluster = await Scoped().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cluster not found.");

        await syncEngine.SyncAsync(cluster, cancellationToken);
        return Project(cluster);
    }

    public async Task<PodLogsDto> GetPodLogsAsync(
        Guid id, string namespaceParameter, string podName, int lines, bool previous, CancellationToken cancellationToken = default)
    {
        var cluster = await Scoped().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cluster not found.");

        var logs = await syncEngine.GetPodLogsAsync(cluster, namespaceParameter, podName, lines, previous, cancellationToken);
        return new PodLogsDto(namespaceParameter, podName, lines, previous, logs);
    }

    public async Task RestartPodAsync(
        Guid id, string namespaceParameter, string podName, CancellationToken cancellationToken = default)
    {
        var cluster = await Scoped().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cluster not found.");

        await syncEngine.RestartPodAsync(cluster, namespaceParameter, podName, cancellationToken);
        await auditLogger.LogAsync(new AuditEntry(
            "kubernetes.pod.restarted", cluster.OrganizationId, currentUser.UserId, currentUser.Email,
            "KubernetesPod", $"{namespaceParameter}/{podName}", cluster.Name), cancellationToken);
    }

    public async Task<KubernetesDeploymentDto> ScaleDeploymentAsync(
        Guid id, string namespaceParameter, string deploymentName, ScaleDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Replicas is < 0 or > 100)
        {
            throw new ValidationException("Replicas must be between 0 and 100.");
        }

        var cluster = await Scoped().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Cluster not found.");

        var (desired, ready) = await syncEngine.ScaleDeploymentAsync(cluster, namespaceParameter, deploymentName, request.Replicas, cancellationToken);

        await auditLogger.LogAsync(new AuditEntry(
            "kubernetes.deployment.scaled", cluster.OrganizationId, currentUser.UserId, currentUser.Email,
            "KubernetesDeployment", $"{namespaceParameter}/{deploymentName}", $"scaled to {request.Replicas}"), cancellationToken);

        return new KubernetesDeploymentDto(Guid.Empty, namespaceParameter, deploymentName, desired, ready, null, DateTime.UtcNow);
    }

    private IQueryable<KubernetesCluster> Scoped() =>
        currentUser.IsSuperAdmin ? db.KubernetesClusters : db.KubernetesClusters.Where(c => c.OrganizationId == currentUser.OrganizationId);

    private static KubernetesClusterDto Project(KubernetesCluster cluster) =>
        new(cluster.Id, cluster.Name, cluster.ApiEndpoint, cluster.IsReachable, cluster.KubernetesVersion,
            cluster.NodeCount, cluster.PodCount, cluster.FailedPodCount, cluster.LastSyncedAtUtc, cluster.LastError, cluster.CreatedAtUtc);
}

public sealed class DockerService(
    OpsPulseDbContext db,
    ICurrentUser currentUser,
    IValidator<CreateDockerHostRequest> createValidator,
    DockerSyncEngine syncEngine) : IDockerService
{
    public async Task<IReadOnlyList<DockerHostDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Scoped()
            .OrderBy(h => h.Name)
            .Select(h => new DockerHostDto(h.Id, h.Name, h.Endpoint, h.IsReachable, h.ContainerCount, h.RunningCount, h.LastSyncedAtUtc, h.LastError, h.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<DockerHostDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var host = await Scoped().FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException("Docker host not found.");

        var containers = await db.DockerContainers.AsNoTracking()
            .Where(c => c.DockerHostId == id)
            .OrderBy(c => c.Name)
            .Select(c => new DockerContainerDto(c.Id, c.ContainerId, c.Name, c.Image, c.State, c.Status, c.CpuUsagePercent, c.MemoryUsedMb, c.MemoryLimitMb, c.StartedAtUtc, c.SyncedAtUtc))
            .ToListAsync(cancellationToken);

        return new DockerHostDetailsDto(
            new DockerHostDto(host.Id, host.Name, host.Endpoint, host.IsReachable, host.ContainerCount, host.RunningCount, host.LastSyncedAtUtc, host.LastError, host.CreatedAtUtc),
            containers);
    }

    public async Task<DockerHostDto> CreateAsync(CreateDockerHostRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateOrThrowAsync(request, cancellationToken);

        var organizationId = currentUser.OrganizationId
            ?? throw new ForbiddenException("Your account has no organization context.");

        if (await db.DockerHosts.AnyAsync(h => h.OrganizationId == organizationId && h.Name == request.Name, cancellationToken))
        {
            throw new ConflictException("A Docker host with this name already exists.");
        }

        var host = new DockerHost
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Endpoint = request.Endpoint.Trim()
        };

        db.DockerHosts.Add(host);
        await db.SaveChangesAsync(cancellationToken);
        await syncEngine.SyncAsync(host, cancellationToken);

        return new DockerHostDto(host.Id, host.Name, host.Endpoint, host.IsReachable, host.ContainerCount, host.RunningCount, host.LastSyncedAtUtc, host.LastError, host.CreatedAtUtc);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var host = await Scoped().FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException("Docker host not found.");

        db.DockerHosts.Remove(host);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DockerHostDto> SyncAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var host = await Scoped().FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            ?? throw new NotFoundException("Docker host not found.");

        await syncEngine.SyncAsync(host, cancellationToken);
        return new DockerHostDto(host.Id, host.Name, host.Endpoint, host.IsReachable, host.ContainerCount, host.RunningCount, host.LastSyncedAtUtc, host.LastError, host.CreatedAtUtc);
    }

    private IQueryable<DockerHost> Scoped() =>
        currentUser.IsSuperAdmin ? db.DockerHosts : db.DockerHosts.Where(h => h.OrganizationId == currentUser.OrganizationId);
}

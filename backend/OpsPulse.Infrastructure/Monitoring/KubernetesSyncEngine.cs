using k8s;
using k8s.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Common;
using OpsPulse.Domain.Kubernetes;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Monitoring;

/// <summary>
/// Synchronizes cluster state (nodes, pods, deployments) from the Kubernetes
/// API and performs control operations (pod logs, pod restart, deployment
/// scaling). Uses an encrypted stored kubeconfig when present, otherwise the
/// local default kubeconfig — least privilege is the operator's RBAC job.
/// </summary>
public sealed class KubernetesSyncEngine(
    OpsPulseDbContext db,
    ICredentialProtector protector,
    ILogger<KubernetesSyncEngine> logger)
{
    public async Task<bool> SyncAsync(KubernetesCluster cluster, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await BuildConfigurationAsync(cluster, cancellationToken);
            using var client = new k8s.Kubernetes(config);

            var nodes = await client.ListNodeAsync(cancellationToken: cancellationToken);
            var pods = await client.ListPodForAllNamespacesAsync(cancellationToken: cancellationToken);
            var deployments = await client.ListDeploymentForAllNamespacesAsync(cancellationToken: cancellationToken);

            var now = DateTime.UtcNow;
            cluster.ApiEndpoint ??= config.Host;
            cluster.KubernetesVersion = nodes.Items
                .Select(n => n.Status?.NodeInfo?.KubeletVersion)
                .FirstOrDefault(v => !string.IsNullOrEmpty(v));
            cluster.IsReachable = true;
            cluster.LastSyncedAtUtc = now;
            cluster.LastError = null;
            cluster.NodeCount = nodes.Items.Count;
            cluster.PodCount = pods.Items.Count;
            cluster.FailedPodCount = pods.Items.Count(p => p.Status?.Phase == "Failed" || p.Status?.Phase == "Unknown");

            var existingNodes = await db.KubernetesNodes.Where(n => n.ClusterId == cluster.Id).ToListAsync(cancellationToken);
            var existingPods = await db.KubernetesPods.Where(p => p.ClusterId == cluster.Id).ToListAsync(cancellationToken);
            var existingDeployments = await db.KubernetesDeployments.Where(d => d.ClusterId == cluster.Id).ToListAsync(cancellationToken);
            db.KubernetesNodes.RemoveRange(existingNodes);
            db.KubernetesPods.RemoveRange(existingPods);
            db.KubernetesDeployments.RemoveRange(existingDeployments);

            foreach (var deployment in deployments.Items)
            {
                db.KubernetesDeployments.Add(new KubernetesDeployment
                {
                    ClusterId = cluster.Id,
                    Namespace = deployment.Metadata?.NamespaceProperty ?? "default",
                    Name = deployment.Metadata?.Name ?? "unknown",
                    ReplicasDesired = (int)(deployment.Spec?.Replicas ?? 0),
                    ReplicasReady = (int)(deployment.Status?.ReadyReplicas ?? 0),
                    Image = deployment.Spec?.Template?.Spec?.Containers?.FirstOrDefault()?.Image,
                    SyncedAtUtc = now
                });
            }

            foreach (var node in nodes.Items)
            {
                var readyCondition = node.Status?.Conditions?
                    .FirstOrDefault(c => c.Type == "Ready");
                var roles = node.Metadata?.Labels?
                    .Where(label => label.Key.StartsWith("node-role.kubernetes.io/", StringComparison.Ordinal))
                    .Select(label => label.Key["node-role.kubernetes.io/".Length..])
                    .ToArray() ?? [];

                db.KubernetesNodes.Add(new KubernetesNode
                {
                    ClusterId = cluster.Id,
                    Name = node.Metadata?.Name ?? "unknown",
                    Status = readyCondition?.Status == "True" ? "Ready" : "NotReady",
                    Roles = roles,
                    KubernetesVersion = node.Status?.NodeInfo?.KubeletVersion,
                    CpuCapacityCores = ParseCpuCores(node.Status?.Capacity?.TryGetValue("cpu", out var cpu) == true ? cpu.ToString() : null),
                    MemoryCapacityMb = ParseMemoryMb(node.Status?.Capacity?.TryGetValue("memory", out var memory) == true ? memory.ToString() : null),
                    SyncedAtUtc = now
                });
            }

            foreach (var pod in pods.Items)
            {
                db.KubernetesPods.Add(new KubernetesPod
                {
                    ClusterId = cluster.Id,
                    Namespace = pod.Metadata?.NamespaceProperty ?? "default",
                    Name = pod.Metadata?.Name ?? "unknown",
                    Status = pod.Status?.Phase ?? "Unknown",
                    RestartCount = (int)(pod.Status?.ContainerStatuses?.Sum(s => s.RestartCount) ?? 0),
                    NodeName = pod.Spec?.NodeName,
                    IsFailed = pod.Status?.Phase is "Failed" or "Unknown",
                    StartedAtUtc = pod.Status?.StartTime,
                    SyncedAtUtc = now
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Cluster {Cluster} synced: {Nodes} nodes, {Pods} pods", cluster.Name, nodes.Items.Count, pods.Items.Count);
            return true;
        }
        catch (Exception exception)
        {
            cluster.IsReachable = false;
            cluster.LastError = exception.Message;
            cluster.LastSyncedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Kubernetes sync failed for {Cluster}: {Message}", cluster.Name, exception.Message);
            return false;
        }
    }

    private async Task<KubernetesClientConfiguration> BuildConfigurationAsync(
        KubernetesCluster cluster, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(cluster.KubeConfigCipher))
        {
            // Prefer in-cluster configuration, then the local default kubeconfig
            // (typical for development against Docker Desktop / Minikube).
            if (Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") is not null)
            {
                return KubernetesClientConfiguration.InClusterConfig();
            }

            return KubernetesClientConfiguration.BuildConfigFromConfigFile();
        }

        // Parse the decrypted kubeconfig from memory — credentials never touch disk.
        var yaml = protector.Unprotect(cluster.KubeConfigCipher);
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(yaml));
        return await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(stream);
    }

    /// <summary>Reads the last <paramref name="tailLines"/> lines of a pod's log output.</summary>
    public async Task<string> GetPodLogsAsync(
        KubernetesCluster cluster,
        string namespaceParameter,
        string podName,
        int tailLines,
        bool previous,
        CancellationToken cancellationToken = default)
    {
        var config = await BuildConfigurationAsync(cluster, cancellationToken);
        using var client = new k8s.Kubernetes(config);

        await using var logStream = await client.ReadNamespacedPodLogAsync(
            podName,
            namespaceParameter,
            container: null,
            follow: false,
            insecureSkipTLSVerifyBackend: null,
            limitBytes: null,
            pretty: null,
            previous: previous,
            sinceSeconds: null,
            tailLines: Math.Clamp(tailLines, 1, 5000),
            timestamps: false,
            cancellationToken: cancellationToken);

        using var reader = new StreamReader(logStream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>Restarts a pod by deleting it; its controller recreates it.</summary>
    public async Task RestartPodAsync(
        KubernetesCluster cluster,
        string namespaceParameter,
        string podName,
        CancellationToken cancellationToken = default)
    {
        var config = await BuildConfigurationAsync(cluster, cancellationToken);
        using var client = new k8s.Kubernetes(config);

        await client.DeleteNamespacedPodAsync(
            podName,
            namespaceParameter,
            new V1DeleteOptions(),
            cancellationToken: cancellationToken);

        logger.LogInformation("Kubernetes pod {Namespace}/{Pod} on {Cluster} deleted for restart",
            namespaceParameter, podName, cluster.Name);
    }

    /// <summary>Scales a deployment to the requested replica count.</summary>
    public async Task<(int Desired, int Ready)> ScaleDeploymentAsync(
        KubernetesCluster cluster,
        string namespaceParameter,
        string deploymentName,
        int replicas,
        CancellationToken cancellationToken = default)
    {
        var config = await BuildConfigurationAsync(cluster, cancellationToken);
        using var client = new k8s.Kubernetes(config);

        var scale = await client.ReadNamespacedDeploymentScaleAsync(deploymentName, namespaceParameter, cancellationToken: cancellationToken);
        scale.Spec.Replicas = Math.Clamp(replicas, 0, 100);
        await client.PatchNamespacedDeploymentScaleAsync(
            new V1Patch(scale, V1Patch.PatchType.MergePatch),
            deploymentName,
            namespaceParameter,
            cancellationToken: cancellationToken);

        logger.LogInformation("Kubernetes deployment {Namespace}/{Deployment} on {Cluster} scaled to {Replicas}",
            namespaceParameter, deploymentName, cluster.Name, replicas);

        return (scale.Spec.Replicas ?? 0, 0);
    }

    private static long ParseCpuCores(string? quantity)
    {
        if (string.IsNullOrEmpty(quantity))
        {
            return 0;
        }

        if (quantity.EndsWith('m'))
        {
            return (long)Math.Ceiling(double.Parse(quantity[..^1]) / 1000.0);
        }

        return long.TryParse(quantity, out var cores) ? cores : 0;
    }

    private static long ParseMemoryMb(string? quantity)
    {
        if (string.IsNullOrEmpty(quantity))
        {
            return 0;
        }

        var multiplier = quantity switch
        {
            _ when quantity.EndsWith("Ki") => 1L << 10,
            _ when quantity.EndsWith("Mi") => 1L << 20,
            _ when quantity.EndsWith("Gi") => 1L << 30,
            _ when quantity.EndsWith("Ti") => 1L << 40,
            _ => 1L
        };

        var numeric = multiplier == 1
            ? quantity
            : quantity[..^2];

        return long.TryParse(numeric, out var value) ? value * multiplier / (1L << 20) : 0;
    }
}

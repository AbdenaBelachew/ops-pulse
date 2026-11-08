using k8s;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Common;
using OpsPulse.Domain.Kubernetes;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Monitoring;

/// <summary>
/// Synchronizes cluster state (version, nodes, pods) from the Kubernetes API.
/// Uses an encrypted stored kubeconfig when present, otherwise the local
/// default kubeconfig — least privilege is the operator's responsibility via RBAC.
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
            db.KubernetesNodes.RemoveRange(existingNodes);
            db.KubernetesPods.RemoveRange(existingPods);

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

        var yaml = protector.Unprotect(cluster.KubeConfigCipher);
        var tempFile = Path.Combine(Path.GetTempPath(), $"opspulse-kubeconfig-{Guid.NewGuid():N}.yaml");
        try
        {
            await File.WriteAllTextAsync(tempFile, yaml, cancellationToken);
            return KubernetesClientConfiguration.BuildConfigFromConfigFile(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
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

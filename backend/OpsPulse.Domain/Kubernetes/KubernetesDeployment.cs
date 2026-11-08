using OpsPulse.Domain.Common;

namespace OpsPulse.Domain.Kubernetes;

/// <summary>
/// A deployment snapshot synced from the cluster, enabling scale operations.
/// </summary>
public class KubernetesDeployment : BaseEntity
{
    public Guid ClusterId { get; set; }

    public KubernetesCluster Cluster { get; set; } = null!;

    public string Namespace { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ReplicasDesired { get; set; }

    public int ReplicasReady { get; set; }

    public string? Image { get; set; }

    public DateTime? SyncedAtUtc { get; set; }
}

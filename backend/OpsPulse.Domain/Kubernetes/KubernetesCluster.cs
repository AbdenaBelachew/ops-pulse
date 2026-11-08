using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Kubernetes;

public class KubernetesCluster : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? ApiEndpoint { get; set; }

    /// <summary>Encrypted kubeconfig YAML. When empty, the local default kubeconfig is used.</summary>
    public string? KubeConfigCipher { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsReachable { get; set; }

    public string? KubernetesVersion { get; set; }

    public int NodeCount { get; set; }

    public int PodCount { get; set; }

    public int FailedPodCount { get; set; }

    public DateTime? LastSyncedAtUtc { get; set; }

    public string? LastError { get; set; }

    public ICollection<KubernetesNode> Nodes { get; set; } = [];

    public ICollection<KubernetesPod> Pods { get; set; } = [];
}

public class KubernetesNode : BaseEntity
{
    public Guid ClusterId { get; set; }

    public KubernetesCluster Cluster { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "Unknown";

    public string[] Roles { get; set; } = [];

    public string? KubernetesVersion { get; set; }

    public long? CpuCapacityCores { get; set; }

    public long? MemoryCapacityMb { get; set; }

    public DateTime? SyncedAtUtc { get; set; }
}

public class KubernetesPod : BaseEntity
{
    public Guid ClusterId { get; set; }

    public KubernetesCluster Cluster { get; set; } = null!;

    public string Namespace { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = "Unknown";

    public int RestartCount { get; set; }

    public string? NodeName { get; set; }

    public bool IsFailed { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? SyncedAtUtc { get; set; }
}

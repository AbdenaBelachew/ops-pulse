using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Domain.Containers;

public class DockerHost : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organizations { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Docker engine endpoint, e.g. npipe://./pipe/docker_engine or unix:///var/run/docker.sock.</summary>
    public string Endpoint { get; set; } = "npipe://./pipe/docker_engine";

    public bool IsActive { get; set; } = true;

    public bool IsReachable { get; set; }

    public int ContainerCount { get; set; }

    public int RunningCount { get; set; }

    public DateTime? LastSyncedAtUtc { get; set; }

    public string? LastError { get; set; }

    public ICollection<DockerContainer> Containers { get; set; } = [];
}

public class DockerContainer : BaseEntity
{
    public Guid DockerHostId { get; set; }

    public DockerHost DockerHost { get; set; } = null!;

    /// <summary>Docker's 64-character container id.</summary>
    public string ContainerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    public string State { get; set; } = "unknown";

    public string Status { get; set; } = string.Empty;

    public double? CpuUsagePercent { get; set; }

    public long? MemoryUsedMb { get; set; }

    public long? MemoryLimitMb { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? SyncedAtUtc { get; set; }
}

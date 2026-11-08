using FluentValidation;
using OpsPulse.Domain.Containers;
using OpsPulse.Domain.Kubernetes;

namespace OpsPulse.Application.InfrastructureMonitor;

public sealed record KubernetesClusterDto(
    Guid Id,
    string Name,
    string? ApiEndpoint,
    bool IsReachable,
    string? KubernetesVersion,
    int NodeCount,
    int PodCount,
    int FailedPodCount,
    DateTime? LastSyncedAtUtc,
    string? LastError,
    DateTime CreatedAtUtc);

public sealed record KubernetesNodeDto(
    Guid Id,
    string Name,
    string Status,
    string[] Roles,
    string? KubernetesVersion,
    long? CpuCapacityCores,
    long? MemoryCapacityMb,
    DateTime? SyncedAtUtc);

public sealed record KubernetesPodDto(
    Guid Id,
    string Namespace,
    string Name,
    string Status,
    int RestartCount,
    string? NodeName,
    bool IsFailed,
    DateTime? StartedAtUtc,
    DateTime? SyncedAtUtc);

public sealed record KubernetesDeploymentDto(
    Guid Id,
    string Namespace,
    string Name,
    int ReplicasDesired,
    int ReplicasReady,
    string? Image,
    DateTime? SyncedAtUtc);

public sealed record KubernetesClusterDetailsDto(
    KubernetesClusterDto Cluster,
    IReadOnlyList<KubernetesNodeDto> Nodes,
    IReadOnlyList<KubernetesPodDto> Pods,
    IReadOnlyList<KubernetesDeploymentDto> Deployments);

public sealed record PodLogsDto(
    string Namespace,
    string Pod,
    int Lines,
    bool Previous,
    string Logs);

public sealed record CreateKubernetesClusterRequest(string Name, string? KubeConfigYaml);

public sealed record ScaleDeploymentRequest(int Replicas);

public sealed record PlatformLogDto(
    Guid Id,
    DateTime TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    string? Source,
    string Process);

public sealed record PlatformLogQuery(
    int Page = 1,
    int PageSize = 100,
    string? Search = null,
    string? Level = null,
    string? Source = null);

public sealed record DockerHostDto(
    Guid Id,
    string Name,
    string Endpoint,
    bool IsReachable,
    int ContainerCount,
    int RunningCount,
    DateTime? LastSyncedAtUtc,
    string? LastError,
    DateTime CreatedAtUtc);

public sealed record DockerContainerDto(
    Guid Id,
    string ContainerId,
    string Name,
    string Image,
    string State,
    string Status,
    double? CpuUsagePercent,
    long? MemoryUsedMb,
    long? MemoryLimitMb,
    DateTime? StartedAtUtc,
    DateTime? SyncedAtUtc);

public sealed record DockerHostDetailsDto(
    DockerHostDto Host,
    IReadOnlyList<DockerContainerDto> Containers);

public sealed record CreateDockerHostRequest(string Name, string Endpoint);

public sealed class CreateKubernetesClusterRequestValidator : AbstractValidator<CreateKubernetesClusterRequest>
{
    public CreateKubernetesClusterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class CreateDockerHostRequestValidator : AbstractValidator<CreateDockerHostRequest>
{
    public CreateDockerHostRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(500)
            .Must(e => e.StartsWith("npipe://") || e.StartsWith("unix://") || e.StartsWith("tcp://") || e.StartsWith("http://") || e.StartsWith("https://"))
            .WithMessage("Endpoint must start with npipe://, unix://, tcp://, http:// or https://.");
    }
}

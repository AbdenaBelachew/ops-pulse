using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsPulse.Domain.Containers;
using OpsPulse.Domain.Databases;
using OpsPulse.Domain.Kubernetes;
using OpsPulse.Domain.Services;
using OpsPulse.Domain.Servers;

namespace OpsPulse.Infrastructure.Persistence.Configurations;

internal sealed class MonitoredServiceConfiguration : IEntityTypeConfiguration<MonitoredService>
{
    public void Configure(EntityTypeBuilder<MonitoredService> builder)
    {
        builder.ToTable("monitored_services", table =>
        {
            table.HasCheckConstraint("ck_monitored_services_check_interval_positive", "check_interval_seconds > 0");
            table.HasCheckConstraint("ck_monitored_services_timeout_positive", "timeout_seconds > 0");
        });

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Url).HasMaxLength(2048).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.Environment).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.CurrentStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.ExpectedStatusCodes).HasMaxLength(100);

        builder.HasOne(s => s.Organization)
            .WithMany(o => o.MonitoredServices)
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.OrganizationId, s.Name }).IsUnique();
        builder.HasIndex(s => new { s.OrganizationId, s.Environment });
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.LastCheckedAtUtc);
    }
}

internal sealed class ServiceCheckConfiguration : IEntityTypeConfiguration<ServiceCheck>
{
    public void Configure(EntityTypeBuilder<ServiceCheck> builder)
    {
        builder.ToTable("service_checks");
        builder.Property(c => c.ErrorMessage).HasMaxLength(500);
        builder.Property(c => c.StatusAfter).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(c => c.Service)
            .WithMany(s => s.Checks)
            .HasForeignKey(c => c.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.ServiceId, c.CheckedAtUtc });
    }
}

internal sealed class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        builder.ToTable("servers");
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Hostname).HasMaxLength(255).IsRequired();
        builder.Property(s => s.IpAddress).HasMaxLength(45);
        builder.Property(s => s.OsName).HasMaxLength(100);
        builder.Property(s => s.OsVersion).HasMaxLength(100);
        builder.Property(s => s.AgentVersion).HasMaxLength(50);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(s => s.Organizations)
            .WithMany()
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.OrganizationId, s.Hostname }).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.LastHeartbeatAtUtc);
    }
}

internal sealed class ServerMetricConfiguration : IEntityTypeConfiguration<ServerMetric>
{
    public void Configure(EntityTypeBuilder<ServerMetric> builder)
    {
        builder.ToTable("server_metrics");

        builder.HasOne(m => m.Server)
            .WithMany(s => s.Metrics)
            .HasForeignKey(m => m.ServerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.ServerId, m.CollectedAtUtc });
    }
}

internal sealed class DatabaseConnectionConfiguration : IEntityTypeConfiguration<DatabaseConnection>
{
    public void Configure(EntityTypeBuilder<DatabaseConnection> builder)
    {
        builder.ToTable("database_connections");
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Host).HasMaxLength(255).IsRequired();
        builder.Property(d => d.DatabaseName).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Username).HasMaxLength(200).IsRequired();
        builder.Property(d => d.CredentialsCipher).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.DatabaseType).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Health).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(d => d.Organizations)
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.OrganizationId, d.Name }).IsUnique();
    }
}

internal sealed class DatabaseMetricConfiguration : IEntityTypeConfiguration<DatabaseMetric>
{
    public void Configure(EntityTypeBuilder<DatabaseMetric> builder)
    {
        builder.ToTable("database_metrics");
        builder.Property(m => m.ErrorMessage).HasMaxLength(500);

        builder.HasOne(m => m.DatabaseConnection)
            .WithMany(d => d.Metrics)
            .HasForeignKey(m => m.DatabaseConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.DatabaseConnectionId, m.CollectedAtUtc });
    }
}

internal sealed class KubernetesClusterConfiguration : IEntityTypeConfiguration<KubernetesCluster>
{
    public void Configure(EntityTypeBuilder<KubernetesCluster> builder)
    {
        builder.ToTable("kubernetes_clusters");
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ApiEndpoint).HasMaxLength(500);
        builder.Property(c => c.KubeConfigCipher).HasMaxLength(100000);
        builder.Property(c => c.KubernetesVersion).HasMaxLength(50);
        builder.Property(c => c.LastError).HasMaxLength(1000);

        builder.HasOne(c => c.Organizations)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.OrganizationId, c.Name }).IsUnique();
    }
}

internal sealed class KubernetesNodeConfiguration : IEntityTypeConfiguration<KubernetesNode>
{
    public void Configure(EntityTypeBuilder<KubernetesNode> builder)
    {
        builder.ToTable("kubernetes_nodes");
        builder.Property(n => n.Name).HasMaxLength(255).IsRequired();
        builder.Property(n => n.Status).HasMaxLength(50);
        builder.Property(n => n.KubernetesVersion).HasMaxLength(50);

        builder.HasOne(n => n.Cluster)
            .WithMany(c => c.Nodes)
            .HasForeignKey(n => n.ClusterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.ClusterId, n.Name }).IsUnique();
    }
}

internal sealed class KubernetesPodConfiguration : IEntityTypeConfiguration<KubernetesPod>
{
    public void Configure(EntityTypeBuilder<KubernetesPod> builder)
    {
        builder.ToTable("kubernetes_pods");
        builder.Property(p => p.Namespace).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(50);
        builder.Property(p => p.NodeName).HasMaxLength(255);

        builder.HasOne(p => p.Cluster)
            .WithMany(c => c.Pods)
            .HasForeignKey(p => p.ClusterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.ClusterId, p.Namespace, p.Name }).IsUnique();
        builder.HasIndex(p => new { p.ClusterId, p.IsFailed });
    }
}

internal sealed class DockerHostConfiguration : IEntityTypeConfiguration<DockerHost>
{
    public void Configure(EntityTypeBuilder<DockerHost> builder)
    {
        builder.ToTable("docker_hosts");
        builder.Property(h => h.Name).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(h => h.LastError).HasMaxLength(1000);

        builder.HasOne(h => h.Organizations)
            .WithMany()
            .HasForeignKey(h => h.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => new { h.OrganizationId, h.Name }).IsUnique();
    }
}

internal sealed class DockerContainerConfiguration : IEntityTypeConfiguration<DockerContainer>
{
    public void Configure(EntityTypeBuilder<DockerContainer> builder)
    {
        builder.ToTable("docker_containers");
        builder.Property(c => c.ContainerId).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(255).IsRequired();
        builder.Property(c => c.Image).HasMaxLength(500).IsRequired();
        builder.Property(c => c.State).HasMaxLength(50);
        builder.Property(c => c.Status).HasMaxLength(200);

        builder.HasOne(c => c.DockerHost)
            .WithMany(h => h.Containers)
            .HasForeignKey(c => c.DockerHostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.DockerHostId, c.ContainerId }).IsUnique();
    }
}

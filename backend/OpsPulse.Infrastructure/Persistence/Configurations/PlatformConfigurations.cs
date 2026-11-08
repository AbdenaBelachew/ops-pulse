using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsPulse.Domain.Logging;
using OpsPulse.Domain.Kubernetes;

namespace OpsPulse.Infrastructure.Persistence.Configurations;

internal sealed class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("system_logs");

        builder.Property(l => l.Level).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Message).HasMaxLength(4000).IsRequired();
        builder.Property(l => l.Exception).HasMaxLength(8000);
        builder.Property(l => l.Source).HasMaxLength(300);
        builder.Property(l => l.Process).HasMaxLength(200);

        builder.HasIndex(l => new { l.TimestampUtc, l.Level });
        builder.HasIndex(l => l.Level);
    }
}

internal sealed class KubernetesDeploymentConfiguration : IEntityTypeConfiguration<KubernetesDeployment>
{
    public void Configure(EntityTypeBuilder<KubernetesDeployment> builder)
    {
        builder.ToTable("kubernetes_deployments");

        builder.Property(d => d.Namespace).HasMaxLength(255).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(255).IsRequired();
        builder.Property(d => d.Image).HasMaxLength(500);

        builder.HasOne(d => d.Cluster)
            .WithMany()
            .HasForeignKey(d => d.ClusterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.ClusterId, d.Namespace, d.Name }).IsUnique();
    }
}

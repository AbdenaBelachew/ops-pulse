using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Audit;
using OpsPulse.Domain.Incidents;
using OpsPulse.Domain.Notifications;
using OpsPulse.Domain.Security;

namespace OpsPulse.Infrastructure.Persistence.Configurations;

internal sealed class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Metric).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Comparator).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Severity).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(r => r.OrganizationId);

        builder.HasOne(r => r.Organizations)
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Services.MonitoredService>()
            .WithMany()
            .HasForeignKey(r => r.TargetServiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class AlertChannelConfiguration : IEntityTypeConfiguration<AlertChannel>
{
    public void Configure(EntityTypeBuilder<AlertChannel> builder)
    {
        builder.ToTable("alert_channels");
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Target).HasMaxLength(500);

        builder.HasOne(c => c.Organizations)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");
        builder.Property(i => i.Title).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.SourceType).HasMaxLength(50).IsRequired();
        builder.Property(i => i.SourceName).HasMaxLength(255);
        builder.Property(i => i.DeduplicationKey).HasMaxLength(200);

        builder.HasOne(i => i.Organizations)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.OrganizationId, i.Status });
        builder.HasIndex(i => i.DeduplicationKey);
        builder.HasIndex(i => i.StartedAtUtc);
    }
}

internal sealed class IncidentEventConfiguration : IEntityTypeConfiguration<IncidentEvent>
{
    public void Configure(EntityTypeBuilder<IncidentEvent> builder)
    {
        builder.ToTable("incident_events");
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.ActorName).HasMaxLength(200);

        builder.HasOne(e => e.Incident)
            .WithMany(i => i.Events)
            .HasForeignKey(e => e.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.IncidentId, e.OccurredAtUtc });
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.Property(n => n.Title).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Target).HasMaxLength(500);
        builder.Property(n => n.DeliveryStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(n => n.Organizations)
            .WithMany()
            .HasForeignKey(n => n.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.OrganizationId, n.UserId, n.IsRead, n.CreatedAtUtc });
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.Property(a => a.UserEmail).HasMaxLength(320);
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100);
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.Details).HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasMaxLength(45);

        builder.HasIndex(a => new { a.OrganizationId, a.OccurredAtUtc });
        builder.HasIndex(a => a.Action);
    }
}

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.Property(k => k.Name).HasMaxLength(200).IsRequired();
        builder.Property(k => k.KeyHash).HasMaxLength(64).IsRequired();
        builder.Property(k => k.KeyPrefix).HasMaxLength(16).IsRequired();

        builder.HasOne(k => k.Organizations)
            .WithMany()
            .HasForeignKey(k => k.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(k => k.KeyHash).IsUnique();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsPulse.Domain.Services;

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

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Url)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Environment)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.ExpectedStatusCodes)
            .HasMaxLength(100);

        builder.HasOne(s => s.Organization)
            .WithMany(o => o.MonitoredServices)
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.OrganizationId, s.Name })
            .IsUnique();

        builder.HasIndex(s => new { s.OrganizationId, s.Environment });

        builder.HasIndex(s => s.IsActive);
    }
}

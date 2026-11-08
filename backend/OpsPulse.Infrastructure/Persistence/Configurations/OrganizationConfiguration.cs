using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsPulse.Domain.Organizations;

namespace OpsPulse.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", table =>
            table.HasCheckConstraint("ck_organizations_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.Description)
            .HasMaxLength(1000);

        builder.HasIndex(o => o.Slug)
            .IsUnique();

        builder.HasIndex(o => o.IsActive);
    }
}

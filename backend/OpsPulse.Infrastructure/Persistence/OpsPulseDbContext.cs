using Microsoft.EntityFrameworkCore;
using OpsPulse.Domain.Common;
using OpsPulse.Domain.Organizations;
using OpsPulse.Domain.Services;
using OpsPulse.Infrastructure.Persistence.Configurations;

namespace OpsPulse.Infrastructure.Persistence;

public sealed class OpsPulseDbContext(DbContextOptions<OpsPulseDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<MonitoredService> MonitoredServices => Set<MonitoredService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OpsPulseDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        TouchAuditableEntries();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchAuditableEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchAuditableEntries()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.UpdatedAtUtc ??= entry.Entity.CreatedAtUtc;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    break;
            }
        }
    }
}

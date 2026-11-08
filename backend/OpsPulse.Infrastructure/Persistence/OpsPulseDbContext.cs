using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Domain.Common;
using OpsPulse.Domain.Identity;
using OpsPulse.Domain.Organizations;
using OpsPulse.Domain.Services;
using OpsPulse.Infrastructure.Identity;
using OpsPulse.Infrastructure.Persistence.Configurations;

namespace OpsPulse.Infrastructure.Persistence;

public sealed class OpsPulseDbContext(DbContextOptions<OpsPulseDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<MonitoredService> MonitoredServices => Set<MonitoredService>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Identity ships with explicit PascalCase table names; override them so the
        // whole schema follows the same snake_case convention. The user table must be
        // renamed on the concrete ApplicationUser type, not its IdentityUser base.
        modelBuilder.Entity<ApplicationUser>().ToTable("asp_net_users");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>().ToTable("asp_net_roles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("asp_net_user_claims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("asp_net_role_claims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("asp_net_user_logins");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("asp_net_user_roles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("asp_net_user_tokens");

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

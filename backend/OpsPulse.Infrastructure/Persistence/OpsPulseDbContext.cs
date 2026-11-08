using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpsPulse.Domain.Audit;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Common;
using OpsPulse.Domain.Containers;
using OpsPulse.Domain.Databases;
using OpsPulse.Domain.Identity;
using OpsPulse.Domain.Incidents;
using OpsPulse.Domain.Kubernetes;
using OpsPulse.Domain.Logging;
using OpsPulse.Domain.Notifications;
using OpsPulse.Domain.Organizations;
using OpsPulse.Domain.Security;
using OpsPulse.Domain.Services;
using OpsPulse.Domain.Servers;
using OpsPulse.Infrastructure.Identity;
using OpsPulse.Infrastructure.Persistence.Configurations;

namespace OpsPulse.Infrastructure.Persistence;

public sealed class OpsPulseDbContext(DbContextOptions<OpsPulseDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<MonitoredService> MonitoredServices => Set<MonitoredService>();
    public DbSet<ServiceCheck> ServiceChecks => Set<ServiceCheck>();
    public DbSet<Server> Servers => Set<Server>();
    public DbSet<ServerMetric> ServerMetrics => Set<ServerMetric>();
    public DbSet<DatabaseConnection> DatabaseConnections => Set<DatabaseConnection>();
    public DbSet<DatabaseMetric> DatabaseMetrics => Set<DatabaseMetric>();
    public DbSet<KubernetesCluster> KubernetesClusters => Set<KubernetesCluster>();
    public DbSet<KubernetesNode> KubernetesNodes => Set<KubernetesNode>();
    public DbSet<KubernetesPod> KubernetesPods => Set<KubernetesPod>();
    public DbSet<KubernetesDeployment> KubernetesDeployments => Set<KubernetesDeployment>();
    public DbSet<DockerHost> DockerHosts => Set<DockerHost>();
    public DbSet<DockerContainer> DockerContainers => Set<DockerContainer>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<AlertChannel> AlertChannels => Set<AlertChannel>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
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

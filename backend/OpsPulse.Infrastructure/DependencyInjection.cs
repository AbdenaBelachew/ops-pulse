using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpsPulse.Application.Agents;
using OpsPulse.Application.Audit;
using OpsPulse.Application.Auth;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Application.Organizations;
using OpsPulse.Application.Users;
using OpsPulse.Infrastructure.Audit;
using OpsPulse.Infrastructure.Identity;
using OpsPulse.Infrastructure.Monitoring;
using OpsPulse.Infrastructure.Notifications;
using OpsPulse.Infrastructure.Persistence;
using OpsPulse.Infrastructure.RealTime;
using OpsPulse.Infrastructure.Security;
using OpsPulse.Infrastructure.Services;

namespace OpsPulse.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure-layer services: EF Core with Npgsql, ASP.NET Core
    /// Identity, JWT issuance, monitoring engines, alerting, auditing and
    /// application services.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OpsPulseDbContext>(options => options
            .UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention());

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<OpsPulseDbContext>();

        // Options.
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MonitoringOptions>(configuration.GetSection(MonitoringOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<EncryptionOptions>(configuration.GetSection(EncryptionOptions.SectionName));

        // Cross-cutting.
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, Security.CurrentUser>();
        services.AddSingleton<ICredentialProtector, CredentialProtector>();
        services.AddScoped<IRealTimeNotifier, SignalRNotifier>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        // Monitoring engines.
        services.AddScoped<SafeHttpCheckEngine>();
        services.AddScoped<ServiceCheckProcessor>();
        services.AddScoped<DatabaseCheckEngine>();
        services.AddScoped<KubernetesSyncEngine>();
        services.AddScoped<DockerSyncEngine>();

        // Alerting.
        services.AddScoped<IIncidentManager, IncidentManager>();
        services.AddScoped<IAlertEvaluator, AlertEvaluator>();

        // Identity.
        services.AddSingleton<JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrganizationService, OrganizationService>();

        // Monitoring services.
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddScoped<IKubernetesService, KubernetesService>();
        services.AddScoped<IDockerService, DockerService>();
        services.AddScoped<IAlertRuleService, AlertRuleService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IAgentIngestService, AgentIngestService>();

        return services;
    }
}

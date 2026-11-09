using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpsPulse.API.Extensions;
using OpsPulse.API.Middleware;
using OpsPulse.Application;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.Common;
using OpsPulse.Domain.Common;
using OpsPulse.Infrastructure;
using OpsPulse.Infrastructure.Identity;
using OpsPulse.Infrastructure.Persistence;
using OpsPulse.Infrastructure.RealTime;
using OpsPulse.Infrastructure.Seed;
using OpsPulse.Infrastructure.Security;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Sink(OpsPulse.Infrastructure.Logging.PostgresLogSink.Instance));

// Application + Infrastructure composition.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Persists log events captured by the Serilog sink into PostgreSQL.
builder.Services.AddHostedService<OpsPulse.Infrastructure.Logging.LogPersistenceService>();

// Presentation: controllers with string enum values in JSON.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Distributed cache + SignalR backplane (both processes reach the same clients).
var redisConnection = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "OpsPulse:";
});

builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnection!, redisOptions =>
    {
        redisOptions.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("OpsPulse:signalr");
    });

// Health checks: API liveness plus readiness of its backing services.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: ["ready"])
    .AddRedis(
        redisConnection!,
        name: "redis",
        tags: ["ready"]);

// OpenAPI document + interactive reference UI.
builder.Services.AddOpenApi();

// Authentication: JWT bearer with short claim names (sub, email, name, org, role).
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        // SignalR WebSockets cannot set headers — the access token arrives in the
        // query string for hub negotiation and connection.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });

// Authorization: policy-based; never rely on UI visibility for security.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanManageUsers, policy => policy.RequireRole(
        RoleNames.SuperAdmin, RoleNames.OrganizationAdmin));

    options.AddPolicy(Policies.CanManageOrganization, policy => policy.RequireRole(
        RoleNames.SuperAdmin, RoleNames.OrganizationAdmin));

    options.AddPolicy(Policies.CanManagePlatform, policy => policy.RequireRole(
        RoleNames.SuperAdmin));

    options.AddPolicy(Policies.CanManageMonitoring, policy => policy.RequireRole(
        RoleNames.SuperAdmin, RoleNames.OrganizationAdmin, RoleNames.DevOpsEngineer));

    options.AddPolicy(Policies.CanOperateMonitoring, policy => policy.RequireRole(
        RoleNames.SuperAdmin, RoleNames.OrganizationAdmin, RoleNames.DevOpsEngineer, RoleNames.Operator));

    options.AddPolicy(Policies.CanManageIncidents, policy => policy.RequireRole(
        RoleNames.SuperAdmin, RoleNames.OrganizationAdmin, RoleNames.DevOpsEngineer, RoleNames.Operator));
});

// Rate limiting: protect authentication endpoints per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1)
        }));
});

const string FrontendCorsPolicy = "FrontendCors";
builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Optional schema migration (enabled for container deployments, where no one runs
// `dotnet ef database update`), then idempotent seed: roles + configured SuperAdmin.
using (var seedScope = app.Services.CreateScope())
{
    if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    {
        app.Logger.LogInformation("Database:MigrateOnStartup is enabled — applying pending migrations");
        await seedScope.ServiceProvider.GetRequiredService<OpsPulseDbContext>().Database.MigrateAsync();
    }

    await DbSeeder.SeedAsync(seedScope.ServiceProvider);
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
app.UseRouting();
// HTTP request count/duration histograms per endpoint, exposed on /metrics.
app.UseHttpMetrics();
app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("OpsPulse API")
        .WithOpenApiRoutePattern("/openapi/{documentName}.json"));
}

app.MapControllers();

app.MapHub<MonitoringHub>("/hubs/monitoring");

// Prometheus-compatible metrics endpoint (Prometheus exposition text format).
app.MapGet("/metrics", async (CancellationToken cancellationToken) =>
{
    using var stream = new MemoryStream();
    await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, cancellationToken);
    var body = Encoding.UTF8.GetString(stream.ToArray());
    return Results.Text(body, "text/plain; version=0.0.4; charset=utf-8");
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteJson
});

app.Run();

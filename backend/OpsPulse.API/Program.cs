using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpsPulse.API.Extensions;
using OpsPulse.API.Middleware;
using OpsPulse.API.Services;
using OpsPulse.Application;
using OpsPulse.Application.Authorization;
using OpsPulse.Application.Common;
using OpsPulse.Domain.Common;
using OpsPulse.Infrastructure;
using OpsPulse.Infrastructure.Identity;
using OpsPulse.Infrastructure.Seed;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext());

// Application + Infrastructure composition.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

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

// Distributed cache backed by Redis (consumed by caching, SignalR backplane and
// rate limiting in later milestones).
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "OpsPulse:";
});

// Health checks: API liveness plus readiness of its backing services.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: ["ready"])
    .AddRedis(
        builder.Configuration.GetConnectionString("Redis")!,
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
    });

// Authorization: policy-based; never rely on UI visibility for security.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanManageUsers, policy => policy.RequireRole(
        RoleNames.SuperAdmin, RoleNames.OrganizationAdmin));

    options.AddPolicy(Policies.CanManageOrganization, policy => policy.RequireRole(
        RoleNames.SuperAdmin, RoleNames.OrganizationAdmin));

    options.AddPolicy(Policies.CanManagePlatform, policy => policy.RequireRole(
        RoleNames.SuperAdmin));
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

// Idempotent seed: roles + configured SuperAdmin.
using (var seedScope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(seedScope.ServiceProvider);
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
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

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteJson
});

app.Run();

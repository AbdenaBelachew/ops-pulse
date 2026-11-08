using Microsoft.AspNetCore.Authentication;
using OpsPulse.Application;
using OpsPulse.Infrastructure;
using OpsPulse.Worker.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(options => options
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Shares the full Infrastructure stack: DbContext, engines, alerting, notifier
// (SignalR events flow through the Redis backplane to API-connected clients).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Identity's SignInManager (used by the shared Infrastructure services) requires
// authentication core services, which the API registers via AddAuthentication().
builder.Services.AddAuthenticationCore();

builder.Services.AddSignalR()
    .AddStackExchangeRedis(
        builder.Configuration.GetConnectionString("Redis")!,
        redisOptions => redisOptions.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("OpsPulse:signalr"));

builder.Services.AddHostedService<ServiceCheckScheduler>();
builder.Services.AddHostedService<AgentHeartbeatMonitor>();
builder.Services.AddHostedService<DatabaseCheckScheduler>();
builder.Services.AddHostedService<KubernetesSyncScheduler>();
builder.Services.AddHostedService<DockerSyncScheduler>();
builder.Services.AddHostedService<RetentionService>();

var host = builder.Build();
host.Run();

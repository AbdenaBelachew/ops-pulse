using OpsPulse.Agent;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(options => options
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddHttpClient();
builder.Services.AddHostedService<AgentService>();

var host = builder.Build();
host.Run();

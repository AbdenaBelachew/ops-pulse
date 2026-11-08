using OpsPulse.Agent;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(options => options
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();

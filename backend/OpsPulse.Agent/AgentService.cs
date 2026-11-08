using System.Net.Http;
using System.Net.Http.Json;
using OpsPulse.Agent.Metrics;

namespace OpsPulse.Agent;

/// <summary>
/// The OpsPulse monitoring agent: authenticates with an API key, sends
/// heartbeats and posts system metric batches on a fixed interval.
/// Configuration (all overridable via environment variables):
///   OpsPulse__ApiUrl   — API base URL (default http://localhost:5000)
///   OpsPulse__ApiKey   — API key created in the OpsPulse UI (required)
///   OpsPulse__IntervalSeconds — collection interval (default 30)
/// </summary>
public sealed class AgentService(
    ILogger<AgentService> logger,
    ILoggerFactory loggerFactory,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : BackgroundService
{
    private readonly string _apiUrl = (configuration["OpsPulse:ApiUrl"] ?? "http://localhost:5000").TrimEnd('/');
    private readonly string _apiKey = configuration["OpsPulse:ApiKey"] ?? string.Empty;
    private readonly int _intervalSeconds = int.TryParse(configuration["OpsPulse:IntervalSeconds"], out var interval) ? interval : 30;

    private Guid? _serverId;
    private readonly SystemMetricsCollector _collector = new(loggerFactory.CreateLogger<SystemMetricsCollector>());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogError("OpsPulse__ApiKey is not configured — the agent cannot start. Create an API key in the OpsPulse UI (Monitoring Agents page).");
            return;
        }

        logger.LogInformation("OpsPulse agent starting — API: {ApiUrl}, interval: {Interval}s", _apiUrl, _intervalSeconds);

        var client = httpClientFactory.CreateClient("agent");
        client.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
        client.Timeout = TimeSpan.FromSeconds(20);

        var (osName, osVersion) = SystemMetricsCollector.GetOs();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_serverId is null)
                {
                    await SendHeartbeatAsync(client, osName, osVersion, stoppingToken);
                }
                else
                {
                    await SendMetricsAsync(client, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Agent cycle failed — will retry");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SendHeartbeatAsync(HttpClient client, string osName, string osVersion, CancellationToken stoppingToken)
    {
        var totalMemoryMb = OperatingSystem.IsWindows()
            ? SystemMetricsCollector.GetTotalMemoryMbWindows()
            : ReadLinuxTotalMemoryMb();

        var heartbeat = new
        {
            hostname = SystemMetricsCollector.GetHostName(),
            name = SystemMetricsCollector.GetHostName(),
            ipAddress = SystemMetricsCollector.GetPrimaryIpAddress(),
            osName,
            osVersion,
            agentVersion = "1.0.0",
            cpuCores = Environment.ProcessorCount,
            totalMemoryMb
        };

        var response = await client.PostAsJsonAsync($"{_apiUrl}/api/v1/agents/heartbeat", heartbeat, stoppingToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<HeartbeatResponse>(cancellationToken: stoppingToken);

        _serverId = result?.ServerId;
        logger.LogInformation("Agent registered as server {ServerId}", _serverId);
    }

    private async Task SendMetricsAsync(HttpClient client, CancellationToken stoppingToken)
    {
        var sample = _collector.Collect();

        var batch = new
        {
            serverId = _serverId,
            metrics = new[]
            {
                new
                {
                    collectedAtUtc = DateTime.UtcNow,
                    cpuUsagePercent = sample.CpuUsagePercent,
                    memoryUsedMb = sample.MemoryUsedMb,
                    memoryTotalMb = sample.MemoryTotalMb,
                    diskUsedPercent = sample.DiskUsedPercent,
                    networkInMbps = sample.NetworkInMbps,
                    networkOutMbps = sample.NetworkOutMbps
                }
            }
        };

        var response = await client.PostAsJsonAsync($"{_apiUrl}/api/v1/agents/metrics", batch, stoppingToken);
        response.EnsureSuccessStatusCode();
        logger.LogDebug("Metrics sent: CPU {Cpu}%, RAM {Used}/{Total} MB", sample.CpuUsagePercent, sample.MemoryUsedMb, sample.MemoryTotalMb);
    }

    private static long ReadLinuxTotalMemoryMb()
    {
        try
        {
            var line = File.ReadAllLines("/proc/meminfo").FirstOrDefault(l => l.StartsWith("MemTotal"));
            return line is null ? 0 : long.Parse(line.Split(':')[1].Trim().Split(' ')[0]) / 1024;
        }
        catch
        {
            return 0;
        }
    }

    private sealed record HeartbeatResponse(Guid ServerId, int RecommendedIntervalSeconds);
}

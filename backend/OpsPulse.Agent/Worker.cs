namespace OpsPulse.Agent;

/// <summary>
/// Standalone monitoring agent placeholder. CPU/RAM/disk/network and database
/// metric collection with secure transmission arrives in Milestone 6.
/// </summary>
public sealed class AgentWorker(ILogger<AgentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OpsPulse agent started — metric collection arrives in Milestone 6");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on graceful shutdown.
        }
    }
}

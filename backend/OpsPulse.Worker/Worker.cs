namespace OpsPulse.Worker;

/// <summary>
/// Monitoring scheduler placeholder. The HTTP check engine, alert evaluation
/// and incident processing arrive in Milestones 3-4 and 8-9.
/// </summary>
public sealed class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OpsPulse monitoring worker started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("Monitoring scheduler idle — check engine arrives in Milestone 4");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on graceful shutdown.
        }
    }
}

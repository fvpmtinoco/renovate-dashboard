// src/RenovateDashboard.App/Workers/DigestWorker.cs
namespace RenovateDashboard.App.Workers;

public class DigestWorker : BackgroundService
{
    private readonly ILogger<DigestWorker> _logger;

    public DigestWorker(ILogger<DigestWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("DigestWorker: waiting for next tick");
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ScraperHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScraperHostedService> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(6); // Kör var 6:e timme

    public ScraperHostedService(
        IServiceProvider serviceProvider,
        ILogger<ScraperHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled scraping run");
                
                using var scope = _serviceProvider.CreateScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<ScraperPipeline>();
                
                var result = await pipeline.RunAllScrapersAsync(
                    maxConcurrency: 5, 
                    cancellationToken: stoppingToken);
                
                _logger.LogInformation($"Scraping completed: {result.TotalEvents} events from {result.SuccessfulScrapers}/{result.TotalScrapers} scrapers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled scraping");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }
    }
}
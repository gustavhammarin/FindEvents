using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class ScraperHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScraperHostedService> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24); // Kör var 6:e timme

    public ScraperHostedService(
        IServiceProvider serviceProvider,
        ILogger<ScraperHostedService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _enabled = configuration.GetValue("Scraper:Enabled", true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Scraper disabled via Scraper:Enabled=false — skipping scheduled runs");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting scheduled scraping run");
                
                using var scope = _serviceProvider.CreateScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<ScraperPipeline>();
                
                var result = await pipeline.RunAllAsync(
                    maxConcurrency: 5,
                    ct: stoppingToken);
                
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
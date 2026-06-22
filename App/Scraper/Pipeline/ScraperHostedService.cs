using App.Embedder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.Scraper.Pipeline;

public class ScraperHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly EventEmbeddingService _backfill;
    private readonly ILogger<ScraperHostedService> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24);

    public ScraperHostedService(
        IServiceProvider serviceProvider,
        EventEmbeddingService backfill,
        ILogger<ScraperHostedService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _backfill = backfill;
        _logger = logger;
        _enabled = configuration.GetValue("Scraper:Enabled", true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Always embed on startup regardless of scraper setting
        await _backfill.RunAsync(stoppingToken);

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

                var result = await pipeline.RunAllAsync(maxConcurrency: 5, ct: stoppingToken);

                _logger.LogInformation("Scraping completed: {Events} events from {Success}/{Total} scrapers",
                    result.TotalEvents, result.SuccessfulScrapers, result.TotalScrapers);

                await _backfill.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled scraping");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }
    }
}

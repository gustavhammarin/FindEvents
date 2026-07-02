using App.Embedder;
using App.Persistence;
using App.Scraper.Models;
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
        await RunStartupEmbedAsync(stoppingToken);

        if (!_enabled)
        {
            _logger.LogInformation("Scraper disabled via Scraper:Enabled=false — skipping scheduled runs");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunScrapeAsync(stoppingToken);

            try { await Task.Delay(_runInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunStartupEmbedAsync(CancellationToken ct)
    {
        try
        {
            var started = DateTime.UtcNow;
            var embed = await _backfill.RunAsync(ct);
            if (!embed.DidWork) return;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.ScrapeRuns.Add(new ScrapeRun
            {
                Trigger = "startup",
                StartedAtUtc = started,
                FinishedAtUtc = DateTime.UtcNow,
                EventsEmbedded = embed.Embedded,
                EmbeddingFailures = embed.Failed,
                EventsReclassified = embed.Reclassified
            });
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup embedding backfill failed");
        }
    }

    private async Task RunScrapeAsync(CancellationToken ct)
    {
        int runId;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var run = new ScrapeRun { Trigger = "scheduled", StartedAtUtc = DateTime.UtcNow };
            db.ScrapeRuns.Add(run);
            await db.SaveChangesAsync(ct);
            runId = run.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Could not create scrape run record — is the database up?");
            return;
        }

        PipelineResult? result = null;
        EmbedRunResult? embed = null;
        string? error = null;
        try
        {
            _logger.LogInformation("Starting scheduled scraping run {RunId}", runId);

            using var scope = _serviceProvider.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<ScraperPipeline>();
            result = await pipeline.RunAllAsync(maxConcurrency: 5, ct: ct);

            _logger.LogInformation("Scraping completed: {Saved} new events from {Success}/{Total} scrapers",
                result.TotalEventsSaved, result.SuccessfulScrapers, result.TotalScrapers);

            embed = await _backfill.RunAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            error = "Avbruten (appen stängdes ner)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled scraping");
            error = ex.Message;
        }

        await FinalizeRunAsync(runId, result, embed, error);
    }

    private async Task FinalizeRunAsync(int runId, PipelineResult? result, EmbedRunResult? embed, string? error)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var run = await db.ScrapeRuns.FindAsync(runId);
            if (run is null) return;

            run.FinishedAtUtc = DateTime.UtcNow;
            run.Error = error;

            if (result is not null)
            {
                run.TotalSources = result.TotalScrapers;
                run.SuccessfulSources = result.SuccessfulScrapers;
                run.EventsFetched = result.TotalEventsFetched;
                run.EventsSaved = result.TotalEventsSaved;
                run.EventsDeleted = result.EventsDeleted;
                run.Sources = result.ScraperResults.Select(r => new ScrapeRunSource
                {
                    SourceName = r.ScraperName,
                    Success = r.Success,
                    EventsFetched = r.EventsFetched,
                    EventsSaved = r.EventsSaved,
                    DurationSeconds = r.DurationSeconds,
                    Error = r.ErrorMessage
                }).ToList();
            }

            if (embed is not null)
            {
                run.EventsEmbedded = embed.Embedded;
                run.EmbeddingFailures = embed.Failed;
                run.EventsReclassified = embed.Reclassified;
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not finalize scrape run {RunId}", runId);
        }
    }
}

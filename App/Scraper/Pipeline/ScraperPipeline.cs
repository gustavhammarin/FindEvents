using App.Repositories;
using App.Scraper.Categorization;
using App.Scraper.Interfaces;
using App.Scraper.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace App.Scraper.Pipeline;

public class ScraperPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScraperPipeline> _logger;

    public ScraperPipeline(IServiceProvider serviceProvider, ILogger<ScraperPipeline> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<PipelineResult> RunAllAsync(int maxConcurrency = 5, CancellationToken ct = default)
    {
        int sourceCount;
        using (var countScope = _serviceProvider.CreateScope())
            sourceCount = countScope.ServiceProvider.GetServices<IEventSource>().Count();

        _logger.LogInformation("Starting pipeline with {Count} sources", sourceCount);

        var semaphore = new SemaphoreSlim(maxConcurrency);
        var tasks = Enumerable.Range(0, sourceCount).Select(i => RunSourceAsync(i, semaphore, ct));
        var results = await Task.WhenAll(tasks);

        var totalEvents = results.Sum(r => r.EventCount);
        _logger.LogInformation("Pipeline done: {Events} events from {Sources}/{Total} sources",
            totalEvents, results.Count(r => r.Success), sourceCount);

        using var cleanupScope = _serviceProvider.CreateScope();
        var repo = cleanupScope.ServiceProvider.GetRequiredService<IEventRepository>();
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow);
        var deleted = await repo.DeleteOldEventsAsync(cutoff);
        if (deleted > 0)
            _logger.LogInformation("Pipeline cleanup: deleted {Count} past events (StartDate < {Cutoff})", deleted, cutoff);

        return new PipelineResult
        {
            TotalScrapers = sourceCount,
            SuccessfulScrapers = results.Count(r => r.Success),
            TotalEvents = totalEvents,
            ScraperResults = results
        };
    }

    private async Task<ScraperResult> RunSourceAsync(int sourceIndex, SemaphoreSlim semaphore, CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        using var scope = _serviceProvider.CreateScope();
        var source = scope.ServiceProvider.GetServices<IEventSource>().ElementAt(sourceIndex);
        try
        {
            _logger.LogInformation("Fetching {Source}", source.Name);
            var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

            const int BatchSize = 10;
            var batch = new List<EventInfo>();
            var seen = new HashSet<(string Title, DateOnly? Date)>();
            int total = 0, submitted = 0;

            await foreach (var ev in source.FetchAsync(ct))
            {
                if (string.IsNullOrEmpty(ev.Link)) continue;
                total++;

                var key = (ev.Title.Trim().ToLowerInvariant(), ev.StartDate);
                if (!seen.Add(key)) continue;

                if (string.IsNullOrWhiteSpace(ev.Category))
                    ev.Category = EventCategorizer.Categorize(ev.Title, ev.Description);
                ev.Municipality = EventMunicipalities.Normalize(ev.Municipality);
                if (string.IsNullOrWhiteSpace(ev.Place))
                    ev.Place = ev.Municipality;

                batch.Add(ev);
                submitted++;

                if (batch.Count >= BatchSize)
                {
                    await repository.SaveEventsAsync(batch, ct);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await repository.SaveEventsAsync(batch, ct);

            _logger.LogInformation("{Source}: {Total} fetched, {Submitted} submitted to save",
                source.Name, total, submitted);
            return new ScraperResult { ScraperName = source.Name, Success = true, EventCount = submitted };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in source {Source}", source.Name);
            return new ScraperResult { ScraperName = source.Name, Success = false, ErrorMessage = ex.Message };
        }
        finally
        {
            semaphore.Release();
        }
    }
}

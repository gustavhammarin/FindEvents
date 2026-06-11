using EventScraper.Categorization;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class ScraperPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScraperPipeline> _logger;

    public ScraperPipeline(
        IServiceProvider serviceProvider,
        ILogger<ScraperPipeline> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<PipelineResult> RunAllAsync(
        int maxConcurrency = 5,
        CancellationToken ct = default)
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

        return new PipelineResult
        {
            TotalScrapers = sourceCount,
            SuccessfulScrapers = results.Count(r => r.Success),
            TotalEvents = totalEvents,
            ScraperResults = results
        };
    }

    private async Task<ScraperResult> RunSourceAsync(
        int sourceIndex,
        SemaphoreSlim semaphore,
        CancellationToken ct)
    {
        await semaphore.WaitAsync(ct);
        // Each source gets its own scope so scoped dependencies (DbContext) aren't shared across threads
        using var scope = _serviceProvider.CreateScope();
        var source = scope.ServiceProvider.GetServices<IEventSource>().ElementAt(sourceIndex);
        try
        {
            _logger.LogInformation("Fetching {Source}", source.Name);
            var events = await source.FetchAsync(ct);
            var list = Deduplicate(events);

            // Structured sources don't go through the LLM — fill their category here
            foreach (var ev in list.Where(e => string.IsNullOrWhiteSpace(e.Category)))
                ev.Category = EventCategorizer.Categorize(ev.Title, ev.Description);

            // Save per source so a crash/restart mid-run never loses completed LLM work
            var repository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            await repository.SaveEventsAsync(list);

            _logger.LogInformation("{Source}: {Count} events saved", source.Name, list.Count);
            return new ScraperResult { ScraperName = source.Name, Success = true, EventCount = list.Count, Events = list };
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

    private static List<EventInfo> Deduplicate(IEnumerable<EventInfo> events) =>
        events
            .Where(e => !string.IsNullOrEmpty(e.Link))
            .GroupBy(e => (Title: e.Title.Trim().ToLowerInvariant(), e.StartDate))
            .Select(g => g.First())
            .ToList();
}

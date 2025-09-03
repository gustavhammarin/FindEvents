using System.Collections.Concurrent;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class ScraperPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileEventRepository _fileEventRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<ScraperPipeline> _logger;
    private readonly ConcurrentBag<EventInfo> _allEvents = new();

    public ScraperPipeline(
        IServiceProvider serviceProvider,
        IFileEventRepository fileEventRepository,
        IEventRepository eventRepository,
        ILogger<ScraperPipeline> logger)
    {
        _serviceProvider = serviceProvider;
        _fileEventRepository = fileEventRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<PipelineResult> RunAllScrapersAsync(
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var scraperTypes = GetAllScraperTypes();
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new List<Task<ScraperResult>>();

        foreach (var scraperType in scraperTypes)
        {
            tasks.Add(RunScraperWithThrottlingAsync(scraperType, semaphore, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        // Processa alla events
        await ProcessEventsAsync(results.SelectMany(r => r.Events));

        return new PipelineResult
        {
            TotalScrapers = scraperTypes.Count,
            SuccessfulScrapers = results.Count(r => r.Success),
            TotalEvents = results.Sum(r => r.EventCount),
            ScraperResults = results
        };
    }

    private async Task<ScraperResult> RunScraperWithThrottlingAsync(
        Type scraperType,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await RunSingleScraperAsync(scraperType, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<ScraperResult> RunSingleScraperAsync(
        Type scraperType,
        CancellationToken cancellationToken)
    {
        var result = new ScraperResult { ScraperName = scraperType.Name };

        try
        {
            _logger.LogInformation($"Starting scraper: {scraperType.Name}");

            using var scope = _serviceProvider.CreateScope();
            var scraper = (BaseScraper)scope.ServiceProvider.GetRequiredService(scraperType);

            var events = await scraper.RunAsync(cancellationToken);

            result.Events = events;
            result.EventCount = events.Count();
            result.Success = true;

            _logger.LogInformation($"Completed {scraperType.Name}: {result.EventCount} events");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, $"Error in scraper {scraperType.Name}");
        }

        return result;
    }

    private async Task ProcessEventsAsync(IEnumerable<EventInfo> events)
    {
        var uniqueEvents = events
            .GroupBy(e => new
            {
                Title = CleanTitle(e.Title),
                e.StartDate
            })
            .Select(g => g.First())
            .ToList();

        await _eventRepository.SaveEventsAsync(uniqueEvents);
        _logger.LogInformation($"Saved {uniqueEvents.Count} unique events to database");
    }

    private string CleanTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return "";

        return title
            .Trim()                           
            .ToLowerInvariant()              
            .Replace("  ", " ")              
            .Replace("\n", " ")              
            .Replace("\r", "")               
            .Replace("\t", " ");             
    }

    private List<Type> GetAllScraperTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(BaseScraper)) && !type.IsAbstract)
            .ToList();
    }
}
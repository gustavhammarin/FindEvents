using System.Text.Json;
using System.Text.Json.Serialization;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;

namespace EventScraper.Sources;

/// <summary>
/// Base class for municipalities that publish events via the lokal.app platform (*.appen.se).
/// Structured API — ISO dates, no LLM needed. Category assigned by pipeline keyword scoring.
/// </summary>
public abstract class LokalAppenSource : IEventSource
{
    private const string CdnBase = "https://cdn.lokal.app/uploads/";

    protected abstract int AppId { get; }
    protected abstract string MunicipalityName { get; }
    protected abstract string AppHost { get; }

    public abstract string Name { get; }

    private readonly IHttpLoader _loader;
    private readonly ILogger _logger;

    protected LokalAppenSource(IHttpLoader loader, ILogger logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async Task<IEnumerable<EventInfo>> FetchAsync(CancellationToken ct = default)
    {
        string json;
        try { json = await _loader.GetStringAsync($"https://api.lokal.app/api/eventsforapp?app_id={AppId}"); }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "{Source}: API request failed", Name);
            return [];
        }

        var items = JsonSerializer.Deserialize<List<LokalEvent>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var events = items
            .Where(e => !string.IsNullOrWhiteSpace(e.Title) && DateOnly.TryParse(e.Date, out var d) && d >= today)
            .Select(Map)
            .ToList();

        _logger.LogInformation("{Source}: {Count} upcoming events", Name, events.Count);
        return events;
    }

    private EventInfo Map(LokalEvent e) => new()
    {
        Title = e.Title!,
        Description = e.Txt,
        Location = e.Place ?? "",
        Municipality = MunicipalityName,
        Source = Name,
        StartDate = DateOnly.TryParse(e.Date, out var d) ? d : null,
        StartTime = TimeOnly.TryParse(e.Starttime, out var st) ? st : null,
        EndTime = e.Showend && TimeOnly.TryParse(e.Endtime, out var et) ? et : null,
        Link = $"https://{AppHost}/evenemang/{e.Id}",
        ImageUrl = e.Media?.SrcThmb != null ? CdnBase + e.Media.SrcThmb : "",
        Category = ""
    };

    private class LokalEvent
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Txt { get; set; }
        public string? Place { get; set; }
        public string? Date { get; set; }
        public string? Starttime { get; set; }
        public string? Endtime { get; set; }
        public bool Showend { get; set; }
        public LokalMedia? Media { get; set; }
    }

    private class LokalMedia
    {
        [JsonPropertyName("src_thmb")]
        public string? SrcThmb { get; set; }
    }
}

public class SavsjoSource(IHttpLoader loader, ILogger<SavsjoSource> logger)
    : LokalAppenSource(loader, logger)
{
    public override string Name => "savsjo.appen.se";
    protected override int AppId => 1;
    protected override string MunicipalityName => "Sävsjö";
    protected override string AppHost => "savsjo.appen.se";
}

public class VetlandaSource(IHttpLoader loader, ILogger<VetlandaSource> logger)
    : LokalAppenSource(loader, logger)
{
    public override string Name => "vetlanda.appen.se";
    protected override int AppId => 2;
    protected override string MunicipalityName => "Vetlanda";
    protected override string AppHost => "vetlanda.appen.se";
}

public class NassjoSource(IHttpLoader loader, ILogger<NassjoSource> logger)
    : LokalAppenSource(loader, logger)
{
    public override string Name => "nassjo.appen.se";
    protected override int AppId => 3;
    protected override string MunicipalityName => "Nässjö";
    protected override string AppHost => "nassjo.appen.se";
}

public class EksjoSource(IHttpLoader loader, ILogger<EksjoSource> logger)
    : LokalAppenSource(loader, logger)
{
    public override string Name => "eksjo.appen.se";
    protected override int AppId => 5;
    protected override string MunicipalityName => "Eksjö";
    protected override string AppHost => "eksjo.appen.se";
}

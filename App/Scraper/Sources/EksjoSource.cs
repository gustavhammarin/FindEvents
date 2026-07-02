using System.Text.Json;
using System.Text.Json.Serialization;
using App.Scraper.Interfaces;
using App.Scraper.Models;

namespace App.Scraper.Sources;

/// <summary>
/// visiteksjo.se (SiteVision) — events come from the ArticleFilterBackend REST API
/// used by the Angular filter app on /upplev/evenemang. Structured JSON, no LLM.
/// </summary>
public class EksjoSource(IHttpLoader loader, ILogger<EksjoSource> logger) : IEventSource
{
    private const string ApiUrl = "https://visiteksjo.se/rest-api/ArticleFilterBackend/?categories%5B%5D=Evenemang";
    private const string SiteBase = "https://visiteksjo.se";

    public string Name => "visiteksjo.se";

    public async IAsyncEnumerable<EventInfo> FetchAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        string json;
        try { json = await loader.GetStringAsync(ApiUrl); }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "{Source}: API request failed", Name);
            yield break;
        }

        var response = JsonSerializer.Deserialize<FilterResponse>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var events = (response?.Articles ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.ArticleName)
                        && DateTime.TryParse(a.StartDate, out var start)
                        && DateOnly.FromDateTime(EndOrStart(a, start)) >= today)
            .ToList();

        logger.LogInformation("{Source}: {Count} upcoming events", Name, events.Count);
        foreach (var article in events)
            yield return Map(article);
    }

    private static DateTime EndOrStart(Article a, DateTime start)
        => DateTime.TryParse(a.EndDate, out var end) && end > start ? end : start;

    private EventInfo Map(Article a)
    {
        var start = DateTime.Parse(a.StartDate!);
        DateTime? end = DateTime.TryParse(a.EndDate, out var e) ? e : null;

        return new EventInfo
        {
            Title = a.ArticleName!,
            Description = a.Ingress,
            Location = a.EvWhere ?? "",
            Municipality = "Eksjö",
            Source = Name,
            StartDate = DateOnly.FromDateTime(start),
            EndDate = end is { } ed && DateOnly.FromDateTime(ed) > DateOnly.FromDateTime(start)
                ? DateOnly.FromDateTime(ed) : null,
            StartTime = start.TimeOfDay > TimeSpan.Zero ? TimeOnly.FromDateTime(start) : null,
            EndTime = end is { } et && et.TimeOfDay > TimeSpan.Zero ? TimeOnly.FromDateTime(et) : null,
            Link = !string.IsNullOrWhiteSpace(a.Url) ? a.Url : SiteBase + a.Uri,
            ImageUrl = string.IsNullOrWhiteSpace(a.Image) ? "" : SiteBase + a.Image,
            Category = ""
        };
    }

    private class FilterResponse
    {
        public List<Article>? Articles { get; set; }
    }

    private class Article
    {
        public string? ArticleName { get; set; }
        public string? Ingress { get; set; }
        public string? EvWhere { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        [JsonPropertyName("URL")]
        public string? Url { get; set; }

        [JsonPropertyName("URI")]
        public string? Uri { get; set; }

        [JsonPropertyName("sv.image")]
        public string? Image { get; set; }
    }
}

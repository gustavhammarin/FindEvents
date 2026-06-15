using System.Text.RegularExpressions;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventScraper.Sources;

/// <summary>
/// Värnamo's event calendar is the external Cruncho service (varnamo.cruncho.co).
/// Two-step API: get-sitemap-infos lists all event ids, /recommendations returns
/// full event data (UTC times) in batches.
/// </summary>
public class VarnamoSource : IEventSource
{
    private const string ApiBase = "https://api-ts.cruncho.co";
    private const string Destination = "varnamo";
    private const int BatchSize = 40;

    private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    private readonly IHttpLoader _loader;
    private readonly ILogger<VarnamoSource> _logger;

    public string Name => "varnamo.cruncho.co";

    public VarnamoSource(IHttpLoader loader, ILogger<VarnamoSource> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async IAsyncEnumerable<EventInfo> FetchAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var infoJson = await _loader.GetStringAsync(
            $"{ApiBase}/eventmanager/events/get-sitemap-infos/{Destination}");
        var infos = JsonConvert.DeserializeObject<List<SitemapInfo>>(infoJson) ?? [];

        var ids = infos
            .Select(i => i._id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Cast<string>()
            .Distinct()
            .ToList();

        _logger.LogInformation("{Source}: {Count} event ids", Name, ids.Count);

        foreach (var chunk in ids.Chunk(BatchSize))
        {
            ct.ThrowIfCancellationRequested();

            var json = await _loader.GetStringAsync(
                $"{ApiBase}/recommendations?ids={string.Join(",", chunk)}&destination={Destination}");
            var recos = JsonConvert.DeserializeObject<List<Recommendation>>(json) ?? [];

            foreach (var reco in recos)
            {
                var ev = MapToEventInfo(reco);
                if (ev is not null) yield return ev;
            }
        }
    }

    private static EventInfo? MapToEventInfo(Recommendation r)
    {
        if (string.IsNullOrEmpty(r.name) || string.IsNullOrEmpty(r.id)) return null;

        // Events can have many occurrences; pick the next upcoming one (or the last past one).
        var occurrences = (r.eventStart ?? [])
            .Select((s, i) => (start: s, end: r.eventEnd?.ElementAtOrDefault(i)))
            .Where(o => o.start != default)
            .OrderBy(o => o.start)
            .ToList();
        if (occurrences.Count == 0) return null;

        var now = DateTimeOffset.UtcNow;
        var chosen = occurrences.FirstOrDefault(o => o.start >= now);
        if (chosen.start == default) chosen = occurrences[^1];

        var start = TimeZoneInfo.ConvertTime(chosen.start, Tz);
        DateTimeOffset? end = chosen.end is { } e && e != default ? TimeZoneInfo.ConvertTime(e, Tz) : null;

        return new EventInfo
        {
            Title = r.name,
            Description = StripHtml(r.description),
            ImageUrl = r.photos?.FirstOrDefault()?.url ?? "",
            Link = $"https://{Destination}.cruncho.co/sv-SE?reco={r.id}",
            StartDate = DateOnly.FromDateTime(start.Date),
            EndDate = end is { } en ? DateOnly.FromDateTime(en.Date) : null,
            StartTime = TimeOnly.FromTimeSpan(start.TimeOfDay),
            EndTime = end is { } en2 ? TimeOnly.FromTimeSpan(en2.TimeOfDay) : null,
            Location = r.eventVenueName ?? r.address ?? "",
            Municipality = "Värnamo",
            Source = "varnamo.cruncho.co",
            Category = ""
        };
    }

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s{2,}", " ").Trim();
        return text.Length > 400 ? text[..400] : text;
    }

    private class SitemapInfo
    {
        public string? _id { get; set; }
    }

    private class Recommendation
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public string? address { get; set; }
        public string? eventVenueName { get; set; }
        public List<DateTimeOffset>? eventStart { get; set; }
        public List<DateTimeOffset>? eventEnd { get; set; }
        public List<Photo>? photos { get; set; }
    }

    private class Photo
    {
        public string? url { get; set; }
    }
}

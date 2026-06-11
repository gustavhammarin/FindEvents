using System.Text.RegularExpressions;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventScraper.Sources;

/// <summary>
/// nassjo.se renders its event archive as a SiteVision portlet whose state
/// (items + hitCount) is embedded as JSON in the page. Paginated via ?p=N, 12 items/page.
/// </summary>
public class NassjoSource : IEventSource
{
    private const string ListUrl = "https://nassjo.se/uppleva-och-gora/evenemang";
    private const string BaseUrl = "https://nassjo.se";
    private const int PageSize = 12;
    private const int MaxPages = 30;

    private static readonly TimeZoneInfo Tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    private readonly IHttpLoader _loader;
    private readonly ILogger<NassjoSource> _logger;

    public string Name => "nassjo.se";

    public NassjoSource(IHttpLoader loader, ILogger<NassjoSource> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async Task<IEnumerable<EventInfo>> FetchAsync(CancellationToken ct = default)
    {
        var events = new List<EventInfo>();
        var seen = new HashSet<string>();

        for (var page = 1; page <= MaxPages; page++)
        {
            ct.ThrowIfCancellationRequested();

            var html = await _loader.GetStringAsync(page == 1 ? ListUrl : $"{ListUrl}?p={page}");
            var state = ExtractArchiveState(html);
            if (state?.items is null || state.items.Count == 0) break;

            foreach (var item in state.items)
            {
                if (string.IsNullOrEmpty(item.URI) || !seen.Add(item.URI)) continue;
                events.Add(MapToEventInfo(item));
            }

            if (events.Count >= state.hitCount || state.items.Count < PageSize) break;
        }

        _logger.LogInformation("{Source}: found {Count} events", Name, events.Count);
        return events;
    }

    private static ArchiveState? ExtractArchiveState(string? html)
    {
        if (string.IsNullOrEmpty(html)) return null;

        foreach (Match m in Regex.Matches(html,
            @"registerInitialState\('[^']+',\s*(\{.*?\})\);", RegexOptions.Singleline))
        {
            var json = m.Groups[1].Value;
            if (!json.Contains("\"hitCount\"")) continue;
            return JsonConvert.DeserializeObject<ArchiveState>(json);
        }

        return null;
    }

    private static EventInfo MapToEventInfo(ArchiveItem item)
    {
        var (startDate, startTime) = FromEpochMs(item.startDate);
        var (endDate, endTime) = FromEpochMs(item.endDate);

        return new EventInfo
        {
            Title = item.displayName ?? "",
            Description = item.text ?? "",
            ImageUrl = Normalize(item.img),
            Link = Normalize(item.URI),
            StartDate = startDate,
            EndDate = endDate,
            StartTime = startTime,
            EndTime = endTime,
            Location = "",
            Municipality = "Nässjö",
            Source = "nassjo.se",
            Category = ""
        };
    }

    private static (DateOnly?, TimeOnly?) FromEpochMs(long? ms)
    {
        if (ms is null or 0) return (null, null);
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(ms.Value), Tz);
        return (DateOnly.FromDateTime(local.Date), TimeOnly.FromTimeSpan(local.TimeOfDay));
    }

    private static string Normalize(string? url) =>
        string.IsNullOrEmpty(url) ? "" :
        url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url :
        BaseUrl + "/" + url.TrimStart('/');

    private class ArchiveState
    {
        public int hitCount { get; set; }
        public List<ArchiveItem> items { get; set; } = [];
    }

    private class ArchiveItem
    {
        public string? displayName { get; set; }
        public string? text { get; set; }
        public string? img { get; set; }
        public string? URI { get; set; }
        public long? startDate { get; set; }
        public long? endDate { get; set; }
    }
}

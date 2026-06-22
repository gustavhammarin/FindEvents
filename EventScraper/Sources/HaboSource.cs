using System.Text.RegularExpressions;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventScraper.Sources;

/// <summary>
/// habokommun.se embeds its event list (Limepark portlet) as JSON in the page.
/// Paginated via ?page&lt;portletId&gt;=N, 6 items/page; portlet id read from paginationHtml.
/// </summary>
public class HaboSource : IEventSource
{
    private const string ListUrl = "https://www.habokommun.se/uppleva-och-gora/upplev-habo/evenemang";
    private const string BaseUrl = "https://www.habokommun.se";
    private const int MaxPages = 20;

    private readonly IHttpLoader _loader;
    private readonly ILogger<HaboSource> _logger;

    public string Name => "habokommun.se";

    public HaboSource(IHttpLoader loader, ILogger<HaboSource> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async IAsyncEnumerable<EventInfo> FetchAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var events = new List<EventInfo>();
        var seen = new HashSet<string>();
        string? pageParam = null;

        for (var page = 1; page <= MaxPages; page++)
        {
            ct.ThrowIfCancellationRequested();

            var url = page == 1 || pageParam is null ? ListUrl : $"{ListUrl}?{pageParam}={page}";
            var html = await _loader.GetStringAsync(url);
            var state = ExtractListState(html);
            if (state?.items is null || state.items.Count == 0) break;

            pageParam ??= ExtractPageParam(state.paginationHtml);

            var newItems = state.items.Where(i => !string.IsNullOrEmpty(i.link) && seen.Add(i.link!)).ToList();
            if (newItems.Count == 0) break;

            foreach (var item in newItems)
                events.Add(MapToEventInfo(item));

            if (events.Count >= state.totalCount || pageParam is null) break;
        }

        _logger.LogInformation("{Source}: found {Count} events", Name, events.Count);
        foreach (var ev in events)
            yield return ev;
    }

    private static ListState? ExtractListState(string? html)
    {
        if (string.IsNullOrEmpty(html)) return null;

        foreach (Match m in Regex.Matches(html,
            @"registerInitialState\('[^']+',\s*(\{.*?\})\);", RegexOptions.Singleline))
        {
            var json = m.Groups[1].Value;
            if (!json.Contains("\"totalCount\"") || !json.Contains("\"items\"")) continue;
            return JsonConvert.DeserializeObject<ListState>(json);
        }

        return null;
    }

    private static string? ExtractPageParam(string? paginationHtml)
    {
        if (string.IsNullOrEmpty(paginationHtml)) return null;
        var m = Regex.Match(paginationHtml, @"[?&](page[\w]+)=\d");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static EventInfo MapToEventInfo(ListItem item)
    {
        return new EventInfo
        {
            Title = item.name ?? "",
            Description = item.description ?? "",
            ImageUrl = Normalize(item.image?.url),
            Link = Normalize(item.link),
            StartDate = ParseDate(item.date?.startDate?.screenreader),
            EndDate = ParseDate(item.date?.endDate?.screenreader),
            Location = "",
            Municipality = "Habo",
            Source = "habokommun.se",
            Category = ""
        };
    }

    private static DateOnly? ParseDate(string? s) =>
        DateOnly.TryParse(s, out var d) ? d : null;

    private static string Normalize(string? url) =>
        string.IsNullOrEmpty(url) ? "" :
        url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url :
        BaseUrl + "/" + url.TrimStart('/');

    private class ListState
    {
        public int totalCount { get; set; }
        public string? paginationHtml { get; set; }
        public List<ListItem> items { get; set; } = [];
    }

    private class ListItem
    {
        public string? name { get; set; }
        public string? description { get; set; }
        public string? link { get; set; }
        public ItemDate? date { get; set; }
        public ItemImage? image { get; set; }
    }

    private class ItemDate
    {
        public DatePart? startDate { get; set; }
        public DatePart? endDate { get; set; }
    }

    private class DatePart
    {
        public string? screenreader { get; set; }
    }

    private class ItemImage
    {
        public string? url { get; set; }
    }
}

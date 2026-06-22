using System.Text.RegularExpressions;
using App.Repositories;
using App.Scraper.Interfaces;
using App.Scraper.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace App.Scraper.Sources;

/// <summary>
/// habokommun.se embeds its event list (Limepark portlet) as JSON in the page.
/// Paginated via ?page&lt;portletId&gt;=N, 6 items/page; portlet id read from paginationHtml.
/// List JSON only has a short teaser — full description fetched from detail page for new events.
/// </summary>
public class HaboSource : IEventSource
{
    private const string ListUrl = "https://www.habokommun.se/uppleva-och-gora/upplev-habo/evenemang";
    private const string BaseUrl = "https://www.habokommun.se";
    private const int MaxPages = 20;

    private readonly IHttpLoader _loader;
    private readonly IEventRepository _repository;
    private readonly ILogger<HaboSource> _logger;

    public string Name => "habokommun.se";

    public HaboSource(IHttpLoader loader, IEventRepository repository, ILogger<HaboSource> logger)
    {
        _loader = loader;
        _repository = repository;
        _logger = logger;
    }

    public async IAsyncEnumerable<EventInfo> FetchAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var allItems = new List<ListItem>();
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

            allItems.AddRange(newItems);

            if (allItems.Count >= state.totalCount || pageParam is null) break;
        }

        _logger.LogInformation("{Source}: found {Count} events", Name, allItems.Count);

        var allLinks = allItems.Select(i => Normalize(i.link)).Where(l => !string.IsNullOrEmpty(l)).ToList();
        var existingLinks = await _repository.GetExistingLinksAsync(allLinks);
        var newLinks = allLinks.Where(l => !existingLinks.Contains(l)).ToHashSet();

        _logger.LogInformation("{Source}: {New} new events — fetching descriptions", Name, newLinks.Count);

        var descriptions = new Dictionary<string, string>();
        if (newLinks.Count > 0)
        {
            var sem = new SemaphoreSlim(3);
            var tasks = allItems
                .Where(i => newLinks.Contains(Normalize(i.link)))
                .Select(async i =>
                {
                    var link = Normalize(i.link);
                    await sem.WaitAsync(ct);
                    try
                    {
                        var desc = await FetchDescriptionAsync(link);
                        if (desc is not null)
                            lock (descriptions) descriptions[link] = desc;
                    }
                    finally { sem.Release(); }
                });
            await Task.WhenAll(tasks);
        }

        foreach (var item in allItems)
        {
            var link = Normalize(item.link);
            descriptions.TryGetValue(link, out var desc);
            yield return MapToEventInfo(item, link, desc ?? item.description ?? "");
        }
    }

    private async Task<string?> FetchDescriptionAsync(string url)
    {
        try
        {
            var doc = await _loader.LoadHtmlAsync(url);
            if (doc is null) return null;

            var nodes = doc.DocumentNode.SelectNodes(
                "//h1/following::p[not(ancestor::nav) and not(ancestor::footer) and not(ancestor::script) and position()<=12]");

            if (nodes is null) return null;

            var parts = nodes
                .Select(p => HtmlEntity.DeEntitize(p.InnerText).Trim())
                .Where(t => t.Length > 10 && !t.StartsWith("window.") && !t.Contains('{'))
                .Distinct()
                .Take(8)
                .ToList();

            return parts.Count > 0 ? string.Join("\n\n", parts) : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "{Source}: failed to fetch description for {Url}", Name, url);
            return null;
        }
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

    private static EventInfo MapToEventInfo(ListItem item, string link, string description)
    {
        return new EventInfo
        {
            Title = item.name ?? "",
            Description = description,
            ImageUrl = Normalize(item.image?.url),
            Link = link,
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

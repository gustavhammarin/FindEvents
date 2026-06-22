using System.Text.RegularExpressions;
using App.Repositories;
using App.Scraper.Interfaces;
using App.Scraper.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace App.Scraper.Sources;

public class JkpgSource : IEventSource
{
    private const string EventListUrl = "https://jkpg.com/evenemang";
    private const string BaseUrl = "https://jkpg.com";
    private const string Municipality = "Jönköping";

    private readonly IHttpLoader _loader;
    private readonly IEventRepository _repository;
    private readonly ILogger<JkpgSource> _logger;

    public string Name => "jkpg.com";

    public JkpgSource(IHttpLoader loader, IEventRepository repository, ILogger<JkpgSource> logger)
    {
        _loader = loader;
        _repository = repository;
        _logger = logger;
    }

    public async IAsyncEnumerable<EventInfo> FetchAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var html = await _loader.GetStringAsync(EventListUrl);
        if (string.IsNullOrEmpty(html))
        {
            _logger.LogWarning("{Source}: failed to load event list", Name);
            yield break;
        }

        var json = ExtractEmbeddedJson(html);
        if (json is null)
        {
            _logger.LogWarning("{Source}: no embedded JSON found", Name);
            yield break;
        }

        var data = JsonConvert.DeserializeObject<JkpgApiResponse>(json);
        if (data?.blocks is null || data.blocks.Count == 0) yield break;

        var blocks = data.blocks
            .Where(b => !string.IsNullOrEmpty(b.link))
            .Select(b => (block: b, link: NormalizeUrl(b.link)))
            .ToList();

        var allLinks = blocks.Select(x => x.link).ToList();
        var existingLinks = await _repository.GetExistingLinksAsync(allLinks);
        var newLinks = blocks
            .Where(x => !existingLinks.Contains(x.link))
            .Select(x => x.link)
            .ToHashSet();

        _logger.LogInformation("{Source}: {Total} events, {New} new — fetching descriptions",
            Name, blocks.Count, newLinks.Count);

        var descriptions = new Dictionary<string, string>();
        if (newLinks.Count > 0)
        {
            var sem = new SemaphoreSlim(3);
            var tasks = blocks
                .Where(x => newLinks.Contains(x.link))
                .Select(async x =>
                {
                    await sem.WaitAsync(ct);
                    try
                    {
                        var desc = await FetchDescriptionAsync(x.link);
                        if (desc is not null)
                            lock (descriptions) descriptions[x.link] = desc;
                    }
                    finally { sem.Release(); }
                });
            await Task.WhenAll(tasks);
        }

        foreach (var (block, link) in blocks)
        {
            descriptions.TryGetValue(link, out var desc);
            yield return MapToEventInfo(block, link, desc ?? "");
        }
    }

    private async Task<string?> FetchDescriptionAsync(string url)
    {
        try
        {
            var doc = await _loader.LoadHtmlAsync(url);
            if (doc is null) return null;

            // Paragraphs in document order after the h1, excluding nav/footer/script
            var nodes = doc.DocumentNode.SelectNodes(
                "//h1/following::p[not(ancestor::nav) and not(ancestor::footer) and not(ancestor::script) and position()<=12]");

            if (nodes is null) return null;

            var parts = nodes
                .Select(p => HtmlEntity.DeEntitize(p.InnerText).Trim())
                .Where(t => t.Length > 20 && !t.StartsWith("window.") && !t.Contains('{'))
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

    private static string? ExtractEmbeddedJson(string html)
    {
        var patterns = new[]
        {
            @"AppRegistry\.registerInitialState\('[^']+',\s*(\{.*?\})\);",
            @"window\.__INITIAL_STATE__\s*=\s*(\{.*?\});",
            @"window\.__DATA__\s*=\s*(\{.*?\});"
        };

        foreach (var pattern in patterns)
        {
            foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
            {
                var json = match.Groups[1].Value;
                if (json.Contains("\"blocks\"")) return json;
            }
        }

        return null;
    }

    private EventInfo MapToEventInfo(JkpgEventBlock b, string link, string description)
    {
        var (startDate, endDate) = ParseDateRange(b.date, b.dateEnd);
        var (startTime, endTime) = ParseTimeRange(b.time, b.timeEnd);

        return new EventInfo
        {
            Title = b.title ?? "",
            Description = description,
            Location = b.location ?? "",
            ImageUrl = NormalizeUrl(b.image),
            Municipality = Municipality,
            Source = Name,
            StartDate = startDate,
            EndDate = endDate,
            StartTime = startTime,
            EndTime = endTime,
            Link = link,
            Category = ""
        };
    }

    private static (DateOnly? start, DateOnly? end) ParseDateRange(string? dateStr, string? dateEndStr)
    {
        DateOnly? start = null, end = null;

        if (!string.IsNullOrEmpty(dateStr))
        {
            var parts = dateStr.Split(" - ", StringSplitOptions.TrimEntries);
            start = ParseDate(parts[0]);
            end = parts.Length > 1 ? ParseDate(parts[1]) : start;
        }

        if (!string.IsNullOrEmpty(dateEndStr))
            end = ParseDate(dateEndStr) ?? end;

        return (start, end);
    }

    private static (TimeOnly? start, TimeOnly? end) ParseTimeRange(string? timeStr, string? timeEndStr)
    {
        TimeOnly? start = null, end = null;

        if (!string.IsNullOrEmpty(timeStr))
        {
            var normalized = NormalizeTimeString(timeStr);
            var parts = normalized.Split('-', StringSplitOptions.TrimEntries);
            start = ParseTime(parts[0]);
            end = parts.Length > 1 ? ParseTime(parts[1]) : null;
        }

        if (!string.IsNullOrEmpty(timeEndStr))
            end = ParseTime(NormalizeTimeString(timeEndStr)) ?? end;

        return (start, end);
    }

    private static string NormalizeTimeString(string s) =>
        s.Replace("–", "-").Replace("—", "-").Replace("−", "-")
         .Replace("kl", "").Replace(".", ":").Trim();

    private static DateOnly? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateOnly.TryParse(s, out var d) ? d : null;
    }

    private static TimeOnly? ParseTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (s.Length == 4 && !s.Contains(':'))
            s = s[..2] + ":" + s[2..];
        return TimeOnly.TryParse(s, out var t) ? t : null;
    }

    private static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
    }

    private class JkpgApiResponse
    {
        public List<JkpgEventBlock> blocks { get; set; } = [];
    }

    private class JkpgEventBlock
    {
        public string? title { get; set; }
        public string? image { get; set; }
        public string? location { get; set; }
        public string? link { get; set; }
        public string? date { get; set; }
        public string? dateEnd { get; set; }
        public string? time { get; set; }
        public string? timeEnd { get; set; }
    }
}

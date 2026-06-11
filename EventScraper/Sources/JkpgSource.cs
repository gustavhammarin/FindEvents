using System.Text.RegularExpressions;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventScraper.Sources;

public class JkpgSource : IEventSource
{
    private const string EventListUrl = "https://jkpg.com/evenemang";
    private const string BaseUrl = "https://jkpg.com";
    private const string Municipality = "Jönköping";

    private readonly IHttpLoader _loader;
    private readonly ILogger<JkpgSource> _logger;

    public string Name => "jkpg.com";

    public JkpgSource(IHttpLoader loader, ILogger<JkpgSource> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async Task<IEnumerable<EventInfo>> FetchAsync(CancellationToken ct = default)
    {
        var html = await _loader.GetStringAsync(EventListUrl);
        if (string.IsNullOrEmpty(html))
        {
            _logger.LogWarning("{Source}: failed to load event list", Name);
            return [];
        }

        var json = ExtractEmbeddedJson(html);
        if (json is null)
        {
            _logger.LogWarning("{Source}: no embedded JSON found", Name);
            return [];
        }

        var data = JsonConvert.DeserializeObject<JkpgApiResponse>(json);
        if (data?.blocks is null || data.blocks.Count == 0) return [];

        _logger.LogInformation("{Source}: found {Count} events", Name, data.blocks.Count);

        return data.blocks
            .Where(b => !string.IsNullOrEmpty(b.link))
            .Select(MapToEventInfo)
            .ToList();
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
            // Page can contain several embedded states (cookie banner, map, event list) —
            // only the event list one has a "blocks" array.
            foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
            {
                var json = match.Groups[1].Value;
                if (json.Contains("\"blocks\"")) return json;
            }
        }

        return null;
    }

    private EventInfo MapToEventInfo(JkpgEventBlock b)
    {
        var link = NormalizeUrl(b.link);
        var (startDate, endDate) = ParseDateRange(b.date, b.dateEnd);
        var (startTime, endTime) = ParseTimeRange(b.time, b.timeEnd);

        return new EventInfo
        {
            Title = b.title ?? "",
            Description = b.description ?? b.ingress ?? "",
            Location = b.location ?? b.locationCity ?? "",
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

    // JSON model
    private class JkpgApiResponse
    {
        public List<JkpgEventBlock> blocks { get; set; } = [];
    }

    private class JkpgEventBlock
    {
        public string? title { get; set; }
        public string? image { get; set; }
        public string? location { get; set; }
        public string? locationCity { get; set; }
        public string? link { get; set; }
        public string? date { get; set; }
        public string? dateEnd { get; set; }
        public string? time { get; set; }
        public string? timeEnd { get; set; }
        public string? description { get; set; }
        public string? ingress { get; set; }
    }
}

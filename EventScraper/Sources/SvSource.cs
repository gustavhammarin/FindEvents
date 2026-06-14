using System.Net;
using System.Text.RegularExpressions;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Utils;
using Microsoft.Extensions.Logging;

namespace EventScraper.Sources;

/// <summary>
/// Studieförbundet Vuxenskolan — events filtered to Jönköpings län.
/// List page is server-side rendered and paginated. Municipality and start date
/// are extracted from the card text (e.g. "Mullsjö sön 2026-08-30") and used as
/// fallback when the LLM cannot extract a date from the event page.
/// </summary>
public class SvSource : IEventSource
{
    private const string BaseUrl = "https://www.sv.se";
    private const string ListUrlBase = "https://www.sv.se/kurser-och-evenemang?g_County=J%C3%B6nk%C3%B6pings+l%C3%A4n&page=";
    private const int MaxPages = 30;

    // Event pages end with a numeric ID, e.g. /kurser-och-evenemang/ovrigt/event-name-123456
    private static readonly Regex HrefRegex = new(
        @"href=""(/kurser-och-evenemang/[^""#?]+-\d{3,})""",
        RegexOptions.Compiled);

    // Captures city + YYYY-MM-DD from card text, e.g. "Mullsjö sön 2026-08-30"
    private static readonly Regex CityDateRegex = new(
        @"([A-ZÅÄÖ][a-zåäö]+(?:\s[A-ZÅÄÖ][a-zåäö]+)*)\s+(?:mån|tis|ons|tor|fre|lör|sön)\s+(\d{4}-\d{2}-\d{2})",
        RegexOptions.Compiled);

    private readonly IHttpLoader _loader;
    private readonly ILlmExtractor _llm;
    private readonly IEventRepository _repository;
    private readonly ILogger<SvSource> _logger;

    public string Name => "sv.se";

    public SvSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repository, ILogger<SvSource> logger)
    {
        _loader = loader;
        _llm = llm;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<EventInfo>> FetchAsync(CancellationToken ct = default)
    {
        var discovered = new List<(string Url, string Municipality, DateOnly? CardDate)>();

        for (var page = 1; page <= MaxPages; page++)
        {
            ct.ThrowIfCancellationRequested();
            string html;
            try { html = await _loader.GetStringAsync(ListUrlBase + page); }
            catch (HttpRequestException) { break; }

            var hrefs = HrefRegex.Matches(html);
            if (hrefs.Count == 0) break;

            foreach (Match m in hrefs)
            {
                var url = BaseUrl + WebUtility.HtmlDecode(m.Groups[1].Value);

                var window = html.Substring(m.Index, Math.Min(600, html.Length - m.Index));
                var cardMatch = CityDateRegex.Match(window);

                var city = cardMatch.Success ? cardMatch.Groups[1].Value.Trim() : "Jönköpings län";
                if (city.Equals("Distans", StringComparison.OrdinalIgnoreCase))
                    city = "Jönköpings län";

                DateOnly? cardDate = cardMatch.Success && DateOnly.TryParse(cardMatch.Groups[2].Value, out var d) ? d : null;

                discovered.Add((url, city, cardDate));
            }
        }

        var urls = discovered.Select(x => x.Url).Distinct().ToList();
        var existing = await _repository.GetLinksBySourceAsync(Name);

        var newItems = discovered
            .DistinctBy(x => x.Url)
            .Where(x => !existing.Contains(x.Url))
            .ToList();

        _logger.LogInformation("{Source}: {Total} event URLs discovered, {Existing} in DB, {New} new",
            Name, urls.Count, existing.Count, newItems.Count);

        var events = new List<EventInfo>();
        foreach (var (url, municipality, cardDate) in newItems)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var doc = await _loader.LoadHtmlAsync(url);
                if (doc is null) continue;

                var text = HtmlTextExtractor.Extract(doc);
                if (string.IsNullOrWhiteSpace(text)) continue;

                if (text.Contains("Evenemanget är avslutat", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Aktiviteten är avslutad", StringComparison.OrdinalIgnoreCase))
                    continue;

                var ev = await _llm.ExtractAsync(text, url, municipality, ct);
                if (ev is null) continue;

                // Use card date as fallback when LLM can't parse complex course schedules
                if (ev.StartDate is null && cardDate.HasValue)
                    ev.StartDate = cardDate;

                // Still skip if we have no date at all
                if (ev.StartDate is null) continue;

                ev.Source = Name;
                if (string.IsNullOrEmpty(ev.ImageUrl))
                    ev.ImageUrl = doc.DocumentNode
                        .SelectSingleNode("//meta[@property='og:image']")
                        ?.GetAttributeValue("content", "") ?? "";

                events.Add(ev);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "{Source}: failed to process {Url}", Name, url);
            }
        }

        return events;
    }
}

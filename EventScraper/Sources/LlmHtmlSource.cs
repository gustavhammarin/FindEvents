using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Utils;
using Microsoft.Extensions.Logging;

namespace EventScraper.Sources;

/// <summary>
/// Generic source for sites without structured event data:
/// discovers event page URLs (sitemap or list page), fetches each page,
/// extracts plain text and lets the LLM produce an EventInfo.
/// Already-saved links are skipped to avoid re-running the LLM.
/// </summary>
public abstract class LlmHtmlSource : IEventSource
{
    private readonly IHttpLoader _loader;
    private readonly ILlmExtractor _llm;
    private readonly IEventRepository _repository;
    private readonly ILogger _logger;

    public abstract string Name { get; }
    protected abstract string Municipality { get; }
    protected abstract string BaseUrl { get; }

    protected LlmHtmlSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repository, ILogger logger)
    {
        _loader = loader;
        _llm = llm;
        _repository = repository;
        _logger = logger;
    }

    protected abstract Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct);

    public async IAsyncEnumerable<EventInfo> FetchAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var urls = (await DiscoverUrlsAsync(ct))
            .Select(NormalizeUrl)
            .Distinct()
            .ToList();

        if (urls.Count == 0)
        {
            _logger.LogWarning("{Source}: no event URLs discovered", Name);
            yield break;
        }

        var existing = await _repository.GetExistingLinksAsync(urls);
        var newUrls = urls.Where(u => !existing.Contains(u)).ToList();
        _logger.LogInformation("{Source}: {Total} URLs, {New} new", Name, urls.Count, newUrls.Count);

        foreach (var url in newUrls)
        {
            if (ct.IsCancellationRequested) yield break;

            EventInfo? ev = null;
            try
            {
                var doc = await _loader.LoadHtmlAsync(url);
                if (doc is null) continue;

                var text = HtmlTextExtractor.Extract(doc);
                if (string.IsNullOrWhiteSpace(text)) continue;

                if (text.Contains("Evenemanget är avslutat", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Aktiviteten är avslutad", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("{Source}: skipping finished event {Url}", Name, url);
                    continue;
                }

                var extracted = await _llm.ExtractAsync(text, url, Municipality, ct);
                if (extracted is null || extracted.StartDate is null)
                {
                    _logger.LogDebug("{Source}: no event extracted from {Url}", Name, url);
                    continue;
                }

                if (extracted.StartDate < DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-14))
                {
                    _logger.LogDebug("{Source}: rejecting stale date {Date} for {Url}", Name, extracted.StartDate, url);
                    continue;
                }

                extracted.Source = Name;
                if (string.IsNullOrEmpty(extracted.ImageUrl))
                    extracted.ImageUrl = ExtractImage(doc);
                extracted.ImageUrl = NormalizeUrl(extracted.ImageUrl);
                ev = extracted;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "{Source}: failed to process {Url}", Name, url);
            }

            if (ev is not null) yield return ev;
        }
    }

    private static string ExtractImage(HtmlAgilityPack.HtmlDocument doc)
    {
        var og = doc.DocumentNode
            .SelectSingleNode("//meta[@property='og:image']")
            ?.GetAttributeValue("content", "");
        if (!string.IsNullOrEmpty(og)) return System.Net.WebUtility.HtmlDecode(og);

        var imgs = doc.DocumentNode.SelectNodes("//img[@src]");
        if (imgs is null) return "";

        var src = imgs
            .Select(i => i.GetAttributeValue("src", ""))
            .FirstOrDefault(s =>
                !string.IsNullOrEmpty(s) &&
                !s.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) &&
                !s.Contains("logo", StringComparison.OrdinalIgnoreCase) &&
                !s.Contains("icon", StringComparison.OrdinalIgnoreCase));

        return System.Net.WebUtility.HtmlDecode(src ?? "");
    }

    protected string NormalizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        return url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? url
            : BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
    }

    /// <summary>Sitemap discovery: loads a (gzipped) sitemap and keeps URLs matching the filter.</summary>
    protected async Task<IEnumerable<string>> FromSitemapAsync(string sitemapUrl, Func<string, bool> filter)
    {
        try
        {
            var doc = await _loader.LoadXmlAsync(sitemapUrl, sitemapUrl.EndsWith(".gz"));
            var ns = doc.Root?.GetDefaultNamespace();
            return doc
                .Descendants(ns is null ? "loc" : ns + "loc")
                .Select(e => e.Value.Trim())
                .Where(filter)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Source}: sitemap fetch failed for {Url}", Name, sitemapUrl);
            return [];
        }
    }

    /// <summary>List page discovery: extracts hrefs matching the regex from a list page.</summary>
    protected async Task<IEnumerable<string>> FromListPageAsync(string listUrl, string hrefPattern)
    {
        var html = await _loader.GetStringAsync(listUrl);
        if (string.IsNullOrEmpty(html)) return [];

        return System.Text.RegularExpressions.Regex
            .Matches(html, $"href=\"({hrefPattern})\"")
            .Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value))
            .ToList();
    }
}

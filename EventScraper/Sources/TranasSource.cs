using System.Text.RegularExpressions;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EventScraper.Sources;

/// <summary>
/// tranas.se exposes events via the WordPress REST API, but date/time/location
/// only exist as free text inside the rendered content — the LLM extracts them.
/// Already-saved links are skipped to avoid re-running the LLM.
/// </summary>
public class TranasSource : IEventSource
{
    private const string ApiUrl = "https://tranas.se/wp-json/wp/v2/event?per_page=100&page=";
    private const int MaxPages = 5;

    private readonly IHttpLoader _loader;
    private readonly ILlmExtractor _llm;
    private readonly IEventRepository _repository;
    private readonly ILogger<TranasSource> _logger;

    public string Name => "tranas.se";

    public TranasSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repository, ILogger<TranasSource> logger)
    {
        _loader = loader;
        _llm = llm;
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<EventInfo>> FetchAsync(CancellationToken ct = default)
    {
        var posts = new List<WpEvent>();
        for (var page = 1; page <= MaxPages; page++)
        {
            ct.ThrowIfCancellationRequested();

            string json;
            try
            {
                json = await _loader.GetStringAsync(ApiUrl + page);
            }
            catch (HttpRequestException)
            {
                break; // WP returns 400 past the last page
            }

            var batch = JsonConvert.DeserializeObject<List<WpEvent>>(json) ?? [];
            if (batch.Count == 0) break;
            posts.AddRange(batch);
            if (batch.Count < 100) break;
        }

        var links = posts.Where(p => !string.IsNullOrEmpty(p.link)).Select(p => p.link!).ToList();
        var existing = await _repository.GetExistingLinksAsync(links);
        var newPosts = posts.Where(p => p.link is not null && !existing.Contains(p.link)).ToList();
        _logger.LogInformation("{Source}: {Total} events, {New} new", Name, posts.Count, newPosts.Count);

        var events = new List<EventInfo>();
        foreach (var post in newPosts)
        {
            if (ct.IsCancellationRequested) break;

            var text = $"{post.title?.rendered}\n{StripHtml(post.content?.rendered)}";
            var ev = await _llm.ExtractAsync(text, post.link!, "Tranås", ct);
            if (ev is null || ev.StartDate is null) continue;

            ev.Source = Name;
            if (string.IsNullOrEmpty(ev.ImageUrl))
                ev.ImageUrl = FirstImage(post.content?.rendered);
            events.Add(ev);
        }

        return events;
    }

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        return Regex.Replace(text, @"\s{2,}", " ").Trim();
    }

    private static string FirstImage(string? html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var m = Regex.Match(html, @"<img[^>]+src=""([^""?]+)");
        return m.Success ? m.Groups[1].Value : "";
    }

    private class WpEvent
    {
        public string? link { get; set; }
        public Rendered? title { get; set; }
        public Rendered? content { get; set; }
    }

    private class Rendered
    {
        public string? rendered { get; set; }
    }
}

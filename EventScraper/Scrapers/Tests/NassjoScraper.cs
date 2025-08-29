using System;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Utils;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace EventScraper.Scrapers.Tests;

public class NassjoScraper : BaseScraper
{
    private readonly SitemapService _sitemapService;
    private const string SitemapIndex = "https://nassjo.se/sitemapindex.xml";

    public NassjoScraper(IHttpLoader loader, SitemapService sitemapService) : base(loader)
    {
        _sitemapService = sitemapService;
    }

    protected override async Task<IEnumerable<string>> GetPageUrlsAsync()
    {
        // Hämtar sitemap-URL:er och filtrerar
        return await _sitemapService.GetEventPageUrlsAsync(
            SitemapIndex,
            url => url.Contains("/uppleva-och-gora/evenemang/evenemang")
        );
    }

    protected override EventInfo ParseEvent(HtmlDocument doc, string url)
    {
        return new EventInfo { Title = doc.DocumentNode.QuerySelector("h1").InnerText.Trim() };
    }
}

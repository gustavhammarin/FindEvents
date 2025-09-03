using System;
using EventScraper.Builders;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Parsers;
using EventScraper.Utils;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace EventScraper.Scrapers;

public class VarnamoScraper(IHttpLoader loader, SitemapService sitemapService) : BaseScraper(loader)
{
    private readonly SitemapService _sitemapService = sitemapService;

    private const string SitemapIndex = "https://www.varnamo.se/sitemapindex.xml";

    private const string BaseUrl = "https://www.varnamo.se";


    protected override async Task<IEnumerable<string>> GetPageUrlsAsync()
    {
        return await _sitemapService.GetEventPageUrlsAsync(
            SitemapIndex,
            url => url.Contains("/varnamose/handerivarnamo/evenemang")

        );
    }

    protected override EventInfo ParseEvent(HtmlDocument doc, string url)
    {
        var builder = new EventDataBuilder();

        var link = url;
        builder.Link = link;

        var title = doc.DocumentNode.QuerySelector("h1.heading")?.InnerText.Trim() ?? "(no title)";

        var date = doc.DocumentNode.QuerySelector("span.vmo-main__eventspage--date")?.InnerText.Trim() ?? "";

        var (startDate, endDate) = DateParser.ParseSwedishDateRange(date);

        builder.StartDate = startDate;
        builder.EndDate = endDate;

        var time = doc.DocumentNode.QuerySelector("span.vmo-main__eventspage--time")?.InnerText.Trim() ?? "";
        var (startTime, endTime) = DateTimeParser.ParseTimes(time);

        builder.StartTime = startTime;
        builder.EndTime = endTime;

        var location = doc.DocumentNode.QuerySelector("span.vmo-main__eventspage--place")?.InnerText.Trim() ?? "";
        builder.Location = location;

        var imgNode = doc.DocumentNode.QuerySelector("img.sv-noborder.c3684");
        var relativeUrl = imgNode?.GetAttributeValue("src","");

        var imageUrl = BaseUrl + relativeUrl;
        builder.ImageUrl = imageUrl;

        var descriptionDiv = doc.DocumentNode.QuerySelector("#Text-0");
        var paragraph = descriptionDiv?.ParentNode?.QuerySelectorAll("p")?.ElementAtOrDefault(0)?.InnerText?.Trim();

        builder.Description = paragraph;
        builder.Municipality = "värnamo";
        builder.Source = BaseUrl;

        return builder.Build(title);
    }
}

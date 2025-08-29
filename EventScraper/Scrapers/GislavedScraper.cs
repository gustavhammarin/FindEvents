using System;
using System.Text.RegularExpressions;
using EventScraper.Builders;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Parsers;
using EventScraper.Utils;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace EventScraper.Scrapers;

public class GislavedScraper(IHttpLoader loader, SitemapService sitemapService) : BaseScraper(loader)
{
    private readonly SitemapService _sitemapService = sitemapService;

    private const string SitemapIndex = "https://www.gislaved.se/sitemapindex.xml";

    private const string BaseUrl = "https://www.gislaved.se";


    protected override async Task<IEnumerable<string>> GetPageUrlsAsync()
    {
        return await _sitemapService.GetEventPageUrlsAsync(
            SitemapIndex,
            url => url.Contains("/evenemangskalender/evenemang")

        );
    }

    protected override EventInfo ParseEvent(HtmlDocument doc, string url)
    {
        var builder = new EventDataBuilder();

        var link = url;
        builder.Link = link;

        var title = doc.DocumentNode.QuerySelector("h1.heading")?.InnerText.Trim() ?? "(no title)";

        var imNode = doc.DocumentNode.SelectSingleNode("//div[@id='Bild-0']/following-sibling::img");

        if (imNode != null)
        {
            var relativeUrl = imNode.GetAttributeValue("src", "");

            if (!string.IsNullOrEmpty(relativeUrl))
            {
                var imageUrl = BaseUrl + relativeUrl;
                builder.ImageUrl = imageUrl;
            }
            else
            {
                Console.WriteLine("Ingen src hittades i img-taggen.");
            }
        }
        else
        {
            Console.WriteLine("Ingen img-tag hittades efter #Bild-0.");
        }


        var dateDiv = doc.DocumentNode.QuerySelector("div.sp-calendar-info");
        var dateText = dateDiv?.ParentNode.QuerySelectorAll("span")?.ElementAtOrDefault(1)?.InnerText.Trim() ?? "";

        var (startdate, endDate) = DateParser.ParseSwedishDateRange(dateText);
        builder.StartDate = startdate;
        builder.EndDate = endDate;

        var days = doc.DocumentNode.QuerySelectorAll(".sp-calendar-info-day");

        foreach (var day in days)
        {
            var spans = day.QuerySelectorAll("span").ToList();

            if (spans.Count >= 2)
            {
                var times = spans[1].InnerText.Trim();

                if (!string.IsNullOrWhiteSpace(times))
                {
                    var (startTime, endTime) = TimeParsers.TimeParser(times);

                    builder.StartTime = startTime;
                    builder.EndTime = endTime;
                }
            }
        }

        var pNode = doc.DocumentNode.QuerySelector("div.sv-text-portlet-content p.normal");

        if (pNode != null)
        {
            var pText = pNode.InnerText.Trim();
            builder.Description = pText;
        }
        else
        {
            Console.WriteLine("Ingen matchande <p>-tagg hittades.");
        }


        builder.Location = "gislaved";

        builder.Municipality = "gislaved";
        builder.Source = BaseUrl;

        return builder.Build(title);
    }
}

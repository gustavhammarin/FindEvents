using System;
using System.Globalization;
using EventScraper.Builders;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Parsers;
using EventScraper.Utils;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Microsoft.Playwright;

namespace EventScraper.Scrapers;

public class EksjoScraper(IHttpLoader loader, SitemapService sitemapService) : BaseScraper(loader)
{
    private readonly SitemapService _sitemapService = sitemapService;

    private const string SitemapIndex = "https://visiteksjo.se/sitemapindex.xml";
    private const string BaseUrl = "https://visiteksjo.se";

    protected override async Task<IEnumerable<string>> GetPageUrlsAsync()
    {
        return await _sitemapService.GetEventPageUrlsAsync(
            SitemapIndex,
            url => url.Contains("/artikelarkiv/evenemang")

        );
    }

    private static readonly string[] formatsArray = new[] { "H.mm", "HH.mm", "H:mm", "HH:mm" };

    protected override EventInfo ParseEvent(HtmlDocument doc, string url)
    {
        var builder = new EventDataBuilder();

        builder.Source = BaseUrl;
        builder.Municipality = "Eksjö";

        builder.Link = url;

        var spans = doc
            .DocumentNode
            .QuerySelectorAll("div.sp-eventInfo > span");

        if (spans != null)
        {
            foreach (var span in spans)
            {
                var img = span.SelectSingleNode(".//img");
                var text = span.InnerText.Trim();

                if (img != null)
                {
                    var src = img.GetAttributeValue("src", "").ToLower();

                    if (src.Contains("calendar"))
                    {
                        var (start, end) = DateParser.ParseSwedishDateRange(text);
                        builder.StartDate = start;
                        builder.EndDate = start == end ? null : end;
                    }

                    else if (src.Contains("clock"))
                    {
                        var (startTime, endTime) = TimeParsers.TimeParser(text);
                        builder.StartTime = startTime;
                        builder.EndTime = endTime;

                    }
                    else if (!src.Contains("location") || src.Contains("location-dot"))
                    {
                        builder.Location = text;
                    }



                }

            }
        }

        var imageNode = doc.DocumentNode.QuerySelector("img[class*='sv-noborder'][srcset]");
        var relativeUrl = imageNode?.GetAttributeValue("src", "");

        if (string.IsNullOrEmpty(relativeUrl))
        {
            var srcset = imageNode?.GetAttributeValue("srcset", "");
            if (!string.IsNullOrEmpty(srcset))
            {
                // Plocka ut den största bilden (sista i srcset)
                var srcsetEntries = srcset.Split(',');
                var lastEntry = srcsetEntries.LastOrDefault()?.Trim();
                if (lastEntry != null)
                {
                    // Ta första delen (före mellanslaget)
                    relativeUrl = lastEntry.Split(' ')[0];
                }
            }
        }

        var imageUrl = BaseUrl + relativeUrl;
        builder.ImageUrl = imageUrl;


        var container = doc.DocumentNode;
        var paragraphs = container
            .QuerySelectorAll("div.sv-text-portlet-content > p")
            .Select(p => p.InnerText.Trim());

        builder.Description = string.Join("\n\n", paragraphs);

        var title = doc.DocumentNode.QuerySelector("h1.heading")?.InnerText.Trim() ?? "(no title)";
        return builder.Build(title);




    }
}

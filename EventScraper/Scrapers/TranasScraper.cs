using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Utils;

namespace EventScraper.Scrapers;

public class TranasScraper(IHttpLoader loader) : BaseScraper(loader)
{
    private const string BaseUrl = "https://www.tranas.se";
    private const string EventListUrl = "https://www.tranas.se/uppleva-och-gora/evenemang";

    protected override async Task<IEnumerable<string>> GetPageUrlsAsync()
    {
        var doc = await Loader.LoadHtmlAsync(EventListUrl);
        if (doc == null)
        {
            Console.WriteLine("Could not load HTML");
            return [];
        }

        var links = new List<string>();

        var archiveDiv = doc.DocumentNode.QuerySelector("div.sv-archive-portlet");

        if (archiveDiv == null)
        {
            Console.WriteLine("❌ Ingen div med .sv-archive-portlet hittad");
            return [];
        }

        var listItems = archiveDiv.QuerySelectorAll("li");


        foreach (var li in listItems)
        {
            var a = li.SelectSingleNode(".//a");
            var href = a?.GetAttributeValue("href", "");
            var text = a?.InnerText.Trim();

            if (href != null)
            {
                if (!href.StartsWith("https"))
                {
                    href = new Uri(new Uri(BaseUrl), href).ToString();
                }
                links.Add(href);
            }


        }
        Console.WriteLine($"found {links.Count} event-links");
        return links;

    }

    protected override EventInfo ParseEvent(HtmlDocument doc, string url)
    {
        var link = url;

        var title = doc.DocumentNode.QuerySelector("h1.heading").InnerText.Trim();

        var paragraph = doc.DocumentNode.QuerySelector("div.sv-text-portlet-content > p");
        var dateTimeNodes = paragraph.QuerySelectorAll("time").ToList();

        var textParts = paragraph.ChildNodes
            .Where(n => n.NodeType == HtmlNodeType.Text || (n.Name != "time" && n.NodeType == HtmlNodeType.Element))
            .Select(n => n.InnerText.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t));

        var location = string.Join(" ", textParts);
        var cleanedLocation = StringParsers.CleanSeparators(location);

        var ingressDiv = doc.DocumentNode.QuerySelector("#Ingress");

        var paragraphs = ingressDiv?.ParentNode.QuerySelectorAll("p") ?? [];
       
        
        var description = string.Join("\n\n", paragraphs.Select(p => p.InnerText.Trim()));
        

        var imgNode = doc.DocumentNode.QuerySelector("img.sv-noborder.c2479");
        var relativeUrl = imgNode?.GetAttributeValue("src","");

        var imageUrl = BaseUrl + relativeUrl; 
        



        DateOnly? startDate = null;
        TimeOnly? startTime = null;
        DateOnly? endDate = null;
        TimeOnly? endTime = null;

        if (dateTimeNodes.Count >= 2)
        {
            var start = dateTimeNodes[0].GetAttributeValue("datetime", "");
            var end = dateTimeNodes[1].GetAttributeValue("datetime", "");


            (startDate, startTime) = DateParser.ParseDateTimeToDateOnlyTimeOnly(DateTime.Parse(start));
            (endDate, endTime) = DateParser.ParseDateTimeToDateOnlyTimeOnly(DateTime.Parse(end));
        }

        return new EventInfo
        {
            Title = title,
            StartDate = startDate,
            StartTime = startTime,
            EndDate = endDate,
            EndTime = endTime,
            Location = cleanedLocation,
            Description = description,
            ImageUrl = imageUrl,
            Link = link,
            Municipality = "Tranås",
            Source = BaseUrl


        };
    }
}
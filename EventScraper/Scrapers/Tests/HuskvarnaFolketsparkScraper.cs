using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventScraper.Builders;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Parsers;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;

namespace EventScraper.Scrapers.Tests
{
    public class HuskvarnaFolketsparkScraper : BaseScraper
    {
        private const string BaseUrl = "https://huskvarnafolketspark.se";
        private const string EventListUrl = "https://huskvarnafolketspark.se/kalender/";

        public HuskvarnaFolketsparkScraper(IHttpLoader loader)
            : base(loader)
        {
        }

        protected override async Task<IEnumerable<string>> GetPageUrlsAsync()
        {
            var doc = await Loader.LoadHtmlAsync(EventListUrl);
            if (doc == null)
            {
                Console.WriteLine("Kunde inte ladda HTML-dokumentet");
                return Enumerable.Empty<string>();
            }

            var links = new List<string>();
            
            // The events are now directly under a links in the main content
            var eventLinks = doc.DocumentNode.SelectNodes("//a[contains(@href, 'kalender/')]");

            if (eventLinks == null)
            {
                Console.WriteLine("Hittade inga event-länkar");
                return Enumerable.Empty<string>();
            }

            foreach (var link in eventLinks)
            {
                var href = link.GetAttributeValue("href", "");
                if (!string.IsNullOrEmpty(href) && href.Contains("/kalender/") && !href.EndsWith("/kalender/"))
                {
                    if (!href.StartsWith("http"))
                    {
                        href = new Uri(new Uri(BaseUrl), href).ToString();
                    }
                    links.Add(href);
                }
            }

            Console.WriteLine($"Hittade {links.Count} event-länkar");
            return links;
        }

        protected override EventInfo ParseEvent(HtmlDocument doc, string url)
        {
            try
            {
                // Hitta datum från förälder-div med font-bold span
                var dateBold = doc.DocumentNode.SelectSingleNode("//span[@class='font-bold']")?.InnerText.Trim() ?? "";
                var (startDate, endDate) = DateTimeParser.ParseDateRange(dateBold);

                // Hitta tid och plats från text-gray-500 span
                var timeLocElem = doc.DocumentNode.SelectSingleNode("//span[@class='text-gray-500']");
                var timeLocation = timeLocElem?.InnerText.Trim() ?? "";
                
                string time = "", location = "";
                if (timeLocation.Contains("–"))
                {
                    var parts = timeLocation.Split('–', 2);
                    time = parts[0].Trim();
                    location = parts.Length > 1 ? parts[1].Trim() : "";
                }
                else
                {
                    time = timeLocation;
                }

                var (startTime, endTime) = DateTimeParser.ParseTimes(time);
                var startDateTime = DateTimeParser.CombineDateAndTime(startDate, startTime);
                var endDateTime = DateTimeParser.CombineDateAndTime(endDate ?? startDate, endTime ?? startTime);

                // Hämta titel från h4
                var title = doc.DocumentNode.SelectSingleNode("//h4")?.InnerText.Trim() ?? "";

                // Hämta bild från img
                var image = doc.DocumentNode.SelectSingleNode("//img")?
                    .GetAttributeValue("src", "") ?? "";

                // Hämta beskrivning från prose div
                var description = doc.DocumentNode.SelectNodes("//div[@class='prose']//p")?
                    .Select(p => p.InnerText.Trim())
                    .Where(text => !string.IsNullOrEmpty(text))
                    .FirstOrDefault() ?? "";

                if (!string.IsNullOrEmpty(image) && !image.StartsWith("http"))
                {
                    image = new Uri(new Uri(BaseUrl), image).ToString();
                }

                return new EventInfo
                {
                    Title = title,
                    Description = description,
                    ImageUrl = image,
                    StartDate = startDate,
                    EndDate = endDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Location = location,
                    Municipality = "Jönköping",
                    Source = "huskvarnafolketspark.se",
                    Link = url,
                    Category = "okategoriserat"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Fel vid parsning av event-sidan: {ex.Message}");
            }
        }
    }
}

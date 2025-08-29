using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EventScraper.Builders;
using EventScraper.Interfaces;
using EventScraper.models;
using EventScraper.Utils;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace EventScraper.Scrapers
{
    public class JkpgScraper : BaseScraper
    {
        private const string BaseUrl = "https://jkpg.com";
        private const string EventListUrl = "https://jkpg.com/evenemang";
        private const string Source = "jkpg.com";
        private const string Municipality = "Jönköping";

        // Cache för JSON-data från huvudsidan
        private JkpgApiResponse? _cachedListData;

        public JkpgScraper(IHttpLoader loader) : base(loader)
        {
        }

        protected override async Task<IEnumerable<string>> GetPageUrlsAsync()
        {
            var html = await GetHtmlStringAsync(EventListUrl);
            if (string.IsNullOrEmpty(html))
            {
                Console.WriteLine("Kunde inte ladda HTML från event-listan");
                return Enumerable.Empty<string>();
            }

            var jsonStr = ExtractJsonFromScript(html);
            if (string.IsNullOrEmpty(jsonStr) || jsonStr == "{}")
            {
                Console.WriteLine("Hittade ingen JSON-data i HTML");
                return Enumerable.Empty<string>();
            }

            try
            {
                _cachedListData = JsonConvert.DeserializeObject<JkpgApiResponse>(jsonStr);
                if (_cachedListData?.blocks == null || !_cachedListData.blocks.Any())
                {
                    Console.WriteLine("Inga events hittades i JSON-data");
                    return Enumerable.Empty<string>();
                }

                Console.WriteLine($"Hittade {_cachedListData.blocks.Count} events");

                // Returnera bara länkar som finns
                var links = _cachedListData.blocks
                    .Where(b => !string.IsNullOrEmpty(b.link))
                    .Select(b => NormalizeUrl(b.link))
                    .Distinct()
                    .ToList();

                Console.WriteLine($"Returnerar {links.Count} unika event-länkar");
                return links;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Fel vid JSON-parsning: {ex.Message}");
                return Enumerable.Empty<string>();
            }
        }

        protected override EventInfo ParseEvent(HtmlDocument doc, string url)
        {
            url = NormalizeUrl(url);

            // Försök först hitta event-data från cachad lista
            var eventBlock = _cachedListData?.blocks?.FirstOrDefault(b => 
                NormalizeUrl(b.link) == url) ?? new JkpgEventBlock();

            // Om vi inte har fullständig data, försök hämta från event-sidan
            if (string.IsNullOrEmpty(eventBlock.description))
            {
                EnrichEventFromDetailPage(doc, eventBlock);
            }

            return MapToEventInfo(eventBlock, url);
        }

        private async Task<string> GetHtmlStringAsync(string url)
        {
            var doc = await Loader.LoadHtmlAsync(url);
            return doc?.DocumentNode?.OuterHtml ?? "";
        }

        private string ExtractJsonFromScript(string html)
        {
            // Primärt mönster med specifikt ID
            var patterns = new[]
            {
                @"AppRegistry\.registerInitialState\('12\.473f1a5e1969a77211b12bd',\s*(\{.*?\})\);",
                @"AppRegistry\.registerInitialState\('[^']+',\s*(\{.*?\})\);",
                @"window\.__INITIAL_STATE__\s*=\s*(\{.*?\});",
                @"window\.__DATA__\s*=\s*(\{.*?\});"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.Singleline);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return "{}";
        }

        private void EnrichEventFromDetailPage(HtmlDocument doc, JkpgEventBlock eventBlock)
        {
            // Hämta beskrivning från paragraf-element
            var paragraphs = doc.DocumentNode.SelectNodes("//p[@class='normal']") 
                          ?? doc.DocumentNode.SelectNodes("//div[contains(@class, 'article')]//p");
            
            if (paragraphs != null && paragraphs.Any())
            {
                eventBlock.description = string.Join(" ", 
                    paragraphs.Select(p => p.InnerText.Trim())
                    .Where(text => !string.IsNullOrEmpty(text)));
            }

            // Om titel saknas, försök hämta från h1
            if (string.IsNullOrEmpty(eventBlock.title))
            {
                var h1 = doc.DocumentNode.SelectSingleNode("//h1");
                if (h1 != null)
                {
                    eventBlock.title = h1.InnerText.Trim();
                }
            }

            // Om bild saknas, försök hämta från img-taggar
            if (string.IsNullOrEmpty(eventBlock.image))
            {
                var img = doc.DocumentNode.SelectSingleNode("//article//img") 
                       ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class, 'event-image')]");
                if (img != null)
                {
                    eventBlock.image = img.GetAttributeValue("src", "");
                }
            }
        }

        private EventInfo MapToEventInfo(JkpgEventBlock block, string url)
        {
            var (dateStart, dateEnd) = ParseDateRange(block.date);
            var (timeStart, timeEnd) = ParseTimeRange(block.time);

            // Använd specifika slutdatum/tid om de finns
            if (!string.IsNullOrEmpty(block.dateEnd))
                dateEnd = block.dateEnd;
            if (!string.IsNullOrEmpty(block.timeEnd))
                timeEnd = block.timeEnd;

            return new EventInfo
            {
                Title = block.title ?? "",
                Description = block.description ?? block.ingress ?? "",
                Location = block.location ?? block.locationCity ?? "",
                ImageUrl = NormalizeUrl(block.image),
                Municipality = Municipality,
                Source = Source,
                StartDate = ParseDate(dateStart),
                EndDate = ParseDate(dateEnd),
                StartTime = ParseTime(timeStart),
                EndTime = ParseTime(timeEnd),
                Link = url,
                Category = block.category ?? ""
            };
        }

        private (string start, string end) ParseDateRange(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return ("", "");

            var parts = dateStr.Split(new[] { " - " }, StringSplitOptions.None);
            var start = parts.Length > 0 ? parts[0].Trim() : "";
            var end = parts.Length > 1 ? parts[1].Trim() : start;
            return (start, end);
        }

        private (string start, string end) ParseTimeRange(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr))
                return ("", "");

            // Normalisera olika typer av bindestreck och formatering
            timeStr = CleanTimeString(timeStr);
            
            var parts = timeStr.Split('-');
            var start = parts.Length > 0 ? parts[0].Trim() : "";
            var end = parts.Length > 1 ? parts[1].Trim() : start;
            
            // Säkerställ format HH:MM
            start = NormalizeTimeFormat(start);
            end = NormalizeTimeFormat(end);
            
            return (start, end);
        }

        private string CleanTimeString(string timeStr)
        {
            return timeStr
                .Replace("–", "-")
                .Replace("—", "-")
                .Replace("−", "-")
                .Replace("kl", "")
                .Replace(".", ":")
                .Trim();
        }

        private string NormalizeTimeFormat(string time)
        {
            if (string.IsNullOrEmpty(time))
                return "";

            // Om tiden redan har kolon, returnera
            if (time.Contains(":"))
                return time;

            // Hantera format som "1030" -> "10:30"
            if (time.Length == 4 && int.TryParse(time, out _))
            {
                return $"{time.Substring(0, 2)}:{time.Substring(2, 2)}";
            }

            return time;
        }

        private DateOnly? ParseDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return null;

            // Försök olika datumformat
            var formats = new[] 
            { 
                "yyyy-MM-dd", 
                "dd/MM/yyyy", 
                "d MMMM yyyy",
                "d MMM yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr, format, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, 
                    out var date))
                {
                    return DateOnly.FromDateTime(date);
                }
            }

            // Fallback till vanlig parsing
            if (DateTime.TryParse(dateStr, out var parsedDate))
            {
                return DateOnly.FromDateTime(parsedDate);
            }

            return null;
        }

        private TimeOnly? ParseTime(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr))
                return null;

            timeStr = NormalizeTimeFormat(timeStr);

            if (TimeOnly.TryParse(timeStr, out var time))
            {
                return time;
            }

            return null;
        }

        private string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
            }

            return url;
        }

        // Interna klasser för JSON-deserialisering
        internal class JkpgApiResponse
        {
            public List<JkpgEventBlock> blocks { get; set; } = new();
        }

        internal class JkpgEventBlock
        {
            public string title { get; set; } = "";
            public string image { get; set; } = "";
            public string location { get; set; } = "";
            public string link { get; set; } = "";
            public string date { get; set; } = "";
            public string dateEnd { get; set; } = "";
            public string time { get; set; } = "";
            public string timeEnd { get; set; } = "";
            public string description { get; set; } = "";
            public string locationCity { get; set; } = "";
            public string locationAddressText { get; set; } = "";
            public string ingress { get; set; } = "";
            public string category { get; set; } = "";
        }
    }
}
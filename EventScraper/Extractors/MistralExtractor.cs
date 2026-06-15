using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventScraper.Categorization;
using EventScraper.Configuration;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventScraper.Extractors;

public class MistralExtractor : ILlmExtractor
{
    private readonly HttpClient _http;
    private readonly ILogger<MistralExtractor> _logger;
    private readonly string _model;

    private const int MaxTextLength = 6000;

    private static readonly string CategoryList =
        string.Join(" | ", EventCategories.Categories);

    public MistralExtractor(HttpClient http, IOptions<MistralSettings> settings, ILogger<MistralExtractor> logger)
    {
        _http = http;
        _model = settings.Value.Model;
        _logger = logger;

        if (!string.IsNullOrEmpty(settings.Value.ApiKey))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.Value.ApiKey);
    }

    public async Task<EventInfo?> ExtractAsync(string text, string sourceUrl, string municipality, CancellationToken ct = default)
    {
        var truncated = text.Length > MaxTextLength ? text[..MaxTextLength] : text;

        var year = DateTime.UtcNow.Year;
        var prompt = $$"""
            Extrahera evenemangsinformation från texten nedan.
            Returnera ENBART ett JSON-objekt, inga förklaringar.
            Om ett fält saknas, sätt det till null.
            Dagens år är {{year}}. Om ett datum saknar år, använd {{year}} (eller {{year + 1}} om datumet redan passerat).

            JSON-schema:
            {
              "title": "sträng (obligatorisk)",
              "startDate": "YYYY-MM-DD eller null",
              "endDate": "YYYY-MM-DD eller null",
              "startTime": "HH:mm eller null",
              "endTime": "HH:mm eller null",
              "location": "platsnamn/adress eller null",
              "place": "stad/ort där evenemanget hålls (t.ex. Huskvarna, Värnamo), 'Distans' om det är online, eller null",
              "municipality": "kommunen i Jönköpings län (t.ex. Jönköping, Habo, Värnamo) eller null",
              "description": "max 400 tecken eller null",
              "imageUrl": "bild-URL om den finns, annars null",
              "category": "exakt en av: {{CategoryList}}"
            }

            Ledtråd: evenemanget är troligen i eller nära kommunen "{{municipality}}".

            Text:
            {{truncated}}
            """;

        var request = new
        {
            model = _model,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.1
        };

        try
        {
            var response = await _http.PostAsync("/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Mistral returned {Status} for {Url}: {Body}", response.StatusCode, sourceUrl, body);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var chatResp = JsonSerializer.Deserialize<ChatCompletionResponse>(json);
            var content = chatResp?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content)) return null;

            var first = content.IndexOf('{');
            var last = content.LastIndexOf('}');
            if (first < 0 || last <= first) return null;
            content = content[first..(last + 1)];

            var extracted = JsonSerializer.Deserialize<ExtractedEvent>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (extracted is null || string.IsNullOrWhiteSpace(extracted.Title)) return null;

            return new EventInfo
            {
                Title = extracted.Title,
                StartDate = ParseDate(extracted.StartDate),
                EndDate = ParseDate(extracted.EndDate),
                StartTime = ParseTime(extracted.StartTime),
                EndTime = ParseTime(extracted.EndTime),
                Location = extracted.Location ?? "",
                Place = string.IsNullOrWhiteSpace(extracted.Place) ? null : extracted.Place.Trim(),
                Municipality = EventMunicipalities.Normalize(extracted.Municipality, municipality),
                Description = extracted.Description,
                ImageUrl = extracted.ImageUrl ?? "",
                Link = sourceUrl,
                Source = new Uri(sourceUrl).Host,
                Category = EventCategorizer.Normalize(extracted.Category)
                    ?? EventCategorizer.Categorize(extracted.Title, extracted.Description)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mistral extraction failed for {Url}", sourceUrl);
            return null;
        }
    }

    public async Task<string?> CategorizeAsync(string title, string? description, CancellationToken ct = default)
    {
        var prompt = $"""
            Vilket kategori passar bäst för detta evenemang?
            Titel: {title}
            Beskrivning: {description ?? ""}

            Välj exakt en av: {CategoryList}
            Svara ENBART med kategorinamnet, inget annat.
            """;

        var request = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.0,
            max_tokens = 30
        };

        try
        {
            var resp = await _http.PostAsync("/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), ct);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync(ct);
            var chatResp = JsonSerializer.Deserialize<ChatCompletionResponse>(json);
            var content = chatResp?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            return string.IsNullOrWhiteSpace(content) ? null : EventCategorizer.Normalize(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mistral categorization failed for '{Title}'", title);
            return null;
        }
    }

    private static DateOnly? ParseDate(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : DateOnly.TryParse(s, out var d) ? d : null;

    private static TimeOnly? ParseTime(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : TimeOnly.TryParse(s, out var t) ? t : null;

    private class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class ExtractedEvent
    {
        public string? Title { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? Location { get; set; }
        public string? Place { get; set; }
        public string? Municipality { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
    }
}

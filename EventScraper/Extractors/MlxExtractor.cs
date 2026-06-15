using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EventScraper.Categorization;
using EventScraper.Configuration;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventScraper.Extractors;

public class MlxExtractor : ILlmExtractor
{
    private readonly HttpClient _http;
    private readonly ILogger<MlxExtractor> _logger;
    private readonly string _model;

    private const int MaxTextLength = 4000;

    private static readonly string CategoryList =
        string.Join(" | ", EventCategories.Categories);

    public MlxExtractor(HttpClient http, IOptions<LlmSettings> settings, ILogger<MlxExtractor> logger)
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
            /no_think
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
              "place": "stad/ort där evenemanget hålls (t.ex. Huskvarna, Värnamo), 'Distans' om det är online, eller null. OBS: I adresser med formatet 'POSTNUMMER STAD, LÄN/REGION' (t.ex. '654 65 Eksjö, Jönköping' eller '331 30 Värnamo, Jönköpings län') är STAD platsen (Eksjö, Värnamo) – inte länet eller regionen som står efter kommat.",
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
            // No response_format: oMLX json_object mode returns garbage (["_output_"])
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            stream = false,
            temperature = 0.1
        };

        try
        {
            var response = await _http.PostAsync("/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MLX returned {Status} for {Url}", response.StatusCode, sourceUrl);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var chatResp = JsonSerializer.Deserialize<ChatCompletionResponse>(json);
            var content = chatResp?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content)) return null;

            // Strip <think>...</think> if present (Qwen3 thinking mode leak)
            content = Regex.Replace(content, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();

            // Keep only the JSON object (model may add code fences or prose)
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
                // Validate LLM answer against the fixed list; keyword scoring as fallback
                Category = EventCategorizer.Normalize(extracted.Category)
                    ?? EventCategorizer.Categorize(extracted.Title, extracted.Description)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MLX extraction failed for {Url}", sourceUrl);
            return null;
        }
    }

    public async Task<string?> CategorizeAsync(string title, string? description, CancellationToken ct = default)
    {
        var prompt = $"""
            /no_think
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
            stream = false,
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
            var content = chatResp?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content)) return null;

            content = Regex.Replace(content, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();
            return string.IsNullOrWhiteSpace(content) ? null : EventCategorizer.Normalize(content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MLX categorization failed for '{Title}'", title);
            return null;
        }
    }

    private static DateOnly? ParseDate(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : DateOnly.TryParse(s, out var d) ? d : null;

    private static TimeOnly? ParseTime(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : TimeOnly.TryParse(s, out var t) ? t : null;

    // OpenAI response model
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

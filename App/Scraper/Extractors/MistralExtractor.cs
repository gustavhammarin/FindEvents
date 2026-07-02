using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using App.Configuration;
using App.Scraper.Categorization;
using App.Scraper.Interfaces;
using App.Scraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Scraper.Extractors;

public class MistralExtractor : ILlmExtractor
{
    private readonly HttpClient _http;
    private readonly MistralRateLimiter _rateLimiter;
    private readonly ILogger<MistralExtractor> _logger;
    private readonly string _model;

    private const int MaxTextLength = 6000;
    private const int MaxAttempts = 4;

    public MistralExtractor(HttpClient http, MistralRateLimiter rateLimiter, IOptions<MistralSettings> settings, ILogger<MistralExtractor> logger)
    {
        _http = http;
        _rateLimiter = rateLimiter;
        _model = settings.Value.CompletionModel;
        _logger = logger;

        if (!string.IsNullOrEmpty(settings.Value.ApiKey))
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.Value.ApiKey);
    }

    public async Task<EventInfo?> ExtractAsync(string text, string sourceUrl, string municipality, CancellationToken ct = default)
    {
        var truncated = text.Length > MaxTextLength ? text[..MaxTextLength] : text;

        var year = DateTime.UtcNow.Year;

        var systemPrompt = $$"""
            Extrahera evenemangsinformation från svenska evenemangssidor.
            Returnera ENBART ett JSON-objekt, inga förklaringar.
            Om ett fält saknas, sätt det till null.

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
              "imageUrl": "bild-URL om den finns, annars null"
            }
            """;

        var userPrompt = $"""
            Dagens år är {year}. Om ett datum saknar år, använd {year} (eller {year + 1} om datumet redan passerat).
            Evenemanget är troligen i eller nära kommunen "{municipality}".

            Text:
            {truncated}
            """;

        var request = new
        {
            model = _model,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.1
        };

        try
        {
            var json = await SendWithRetryAsync(request, sourceUrl, ct);
            if (json is null) return null;
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
                Category = EventCategorizer.Categorize(extracted.Title, extracted.Description)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mistral extraction failed for {Url}", sourceUrl);
            return null;
        }
    }

    /// <summary>Sends the chat completion request, retrying 429/5xx/timeouts with backoff. Returns the response body or null.</summary>
    private async Task<string?> SendWithRetryAsync(object request, string sourceUrl, CancellationToken ct)
    {
        var backoff = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await _rateLimiter.WaitAsync(ct);

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync("/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"), ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                _logger.LogWarning("Mistral request failed ({Error}) for {Url}, attempt {Attempt}/{Max}",
                    ex.Message, sourceUrl, attempt, MaxAttempts);
                if (attempt == MaxAttempts) return null;
                await Task.Delay(backoff, ct);
                backoff *= 2;
                continue;
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync(ct);

                var status = (int)response.StatusCode;
                var retryable = status == 429 || status >= 500;
                if (!retryable || attempt == MaxAttempts)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Mistral returned {Status} for {Url}: {Body}", response.StatusCode, sourceUrl, body);
                    return null;
                }

                var delay = response.Headers.RetryAfter?.Delta ?? backoff;
                if (status == 429)
                    _rateLimiter.Penalize(delay);
                _logger.LogDebug("Mistral {Status} for {Url}, retrying in {Delay}s (attempt {Attempt}/{Max})",
                    status, sourceUrl, delay.TotalSeconds, attempt, MaxAttempts);
                await Task.Delay(delay, ct);
                backoff *= 2;
            }
        }

        return null;
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
    }
}

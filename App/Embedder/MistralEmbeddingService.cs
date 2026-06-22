using App.Configuration;
using App.Persistence;
using Microsoft.Extensions.Options;
using Pgvector;

namespace App.Embedder;

public class MistralEmbeddingService(HttpClient http, IOptions<MistralSettings> settings, ILogger<MistralEmbeddingService> logger)
{
    private readonly string _model = settings.Value.EmbeddingModel;

    public async Task<Vector?> EmbedAsync(string text, CancellationToken ct = default)
    {
        var results = await EmbedBatchAsync([text], ct);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<List<Vector?>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var delay = TimeSpan.FromSeconds(2);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var response = await http.PostAsJsonAsync("v1/embeddings", new
                {
                    model = _model,
                    input = texts.Select(Truncate).ToArray()
                }, ct);

                if ((int)response.StatusCode == 429)
                {
                    logger.LogDebug("Mistral 429 rate limit, waiting {Delay}s (attempt {Attempt})", delay.TotalSeconds, attempt + 1);
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
                return result?.Data
                    .OrderBy(d => d.Index)
                    .Select(d => (Vector?)new Vector(d.Embedding))
                    .ToList() ?? [];
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mistral embed batch failed, count={Count}", texts.Count);
                return Enumerable.Repeat<Vector?>(null, texts.Count).ToList();
            }
        }

        logger.LogWarning("Mistral embed gave up after 5 attempts (rate limited), count={Count}", texts.Count);
        return Enumerable.Repeat<Vector?>(null, texts.Count).ToList();
    }

    public static string BuildEventText(Event ev) =>
        string.Join(" | ", new[]
        {
            ev.Title,
            ev.Category,
            ev.Municipality,
            ev.Place,
            ev.Location,
            ev.Description
        }.Where(s => !string.IsNullOrEmpty(s)));

    private static string Truncate(string text) => text.Length > 8000 ? text[..8000] : text;
    private record EmbeddingResponse(List<EmbeddingData> Data);
    private record EmbeddingData(int Index, float[] Embedding);
}

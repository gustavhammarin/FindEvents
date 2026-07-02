using App.Configuration;
using App.Persistence;
using Microsoft.Extensions.Options;
using Pgvector;

namespace App.Embedder;

public class MistralEmbeddingService(
    HttpClient http,
    MistralRateLimiter rateLimiter,
    IOptions<MistralSettings> settings,
    ILogger<MistralEmbeddingService> logger)
{
    private readonly string _model = settings.Value.EmbeddingModel;
    private const int MaxAttempts = 5;

    public async Task<Vector?> EmbedAsync(string text, CancellationToken ct = default)
    {
        var results = await EmbedBatchAsync([text], ct);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<List<Vector?>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var backoff = TimeSpan.FromSeconds(2);
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await rateLimiter.WaitAsync(ct);

                var response = await http.PostAsJsonAsync("v1/embeddings", new
                {
                    model = _model,
                    input = texts.Select(Truncate).ToArray()
                }, ct);

                var status = (int)response.StatusCode;
                if (status == 429 || status >= 500)
                {
                    var delay = response.Headers.RetryAfter?.Delta ?? backoff;
                    if (status == 429)
                        rateLimiter.Penalize(delay);
                    logger.LogDebug("Mistral {Status} on embeddings, waiting {Delay}s (attempt {Attempt}/{Max})",
                        status, delay.TotalSeconds, attempt, MaxAttempts);
                    await Task.Delay(delay, ct);
                    backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 60));
                    continue;
                }

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
                return result?.Data
                    .OrderBy(d => d.Index)
                    .Select(d => (Vector?)new Vector(d.Embedding))
                    .ToList() ?? [];
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Mistral embed batch failed (attempt {Attempt}/{Max}), count={Count}",
                    attempt, MaxAttempts, texts.Count);
                if (attempt == MaxAttempts) break;
                await Task.Delay(backoff, ct);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 60));
            }
        }

        logger.LogWarning("Mistral embed gave up after {Max} attempts, count={Count}", MaxAttempts, texts.Count);
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

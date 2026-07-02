using App.Persistence;
using Microsoft.EntityFrameworkCore;

namespace App.Embedder;

public record EmbedRunResult(int Embedded, int Failed, int Reclassified)
{
    public bool DidWork => Embedded > 0 || Failed > 0 || Reclassified > 0;
}

public class EventEmbeddingService(
    IServiceScopeFactory scopeFactory,
    CategoryClassifierService classifier,
    ILogger<EventEmbeddingService> logger)
{
    private const int BatchSize = 50;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public async Task<EmbedRunResult> RunAsync(CancellationToken ct = default)
    {
        if (!await _runLock.WaitAsync(0, ct))
        {
            logger.LogDebug("Embedding backfill already running — skipping");
            return new EmbedRunResult(0, 0, 0);
        }
        try { return await BackfillAsync(ct); }
        finally { _runLock.Release(); }
    }

    private async Task<EmbedRunResult> BackfillAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embedder = scope.ServiceProvider.GetRequiredService<MistralEmbeddingService>();

        var unembedded = await db.Events
            .Where(e => e.Embedding == null)
            .ToListAsync(ct);

        int embedded = 0, failed = 0;
        var newlyEmbedded = new List<Event>();
        if (unembedded.Count > 0)
        {
            logger.LogInformation("Embedding {Count} events in batches of {BatchSize}", unembedded.Count, BatchSize);

            for (var i = 0; i < unembedded.Count; i += BatchSize)
            {
                if (ct.IsCancellationRequested) break;

                var batch = unembedded.Skip(i).Take(BatchSize).ToList();
                var texts = batch.Select(MistralEmbeddingService.BuildEventText).ToList();
                var vectors = await embedder.EmbedBatchAsync(texts, ct);

                for (var j = 0; j < batch.Count; j++)
                {
                    batch[j].Embedding = vectors[j];
                    if (vectors[j] is null)
                    {
                        failed++;
                    }
                    else
                    {
                        embedded++;
                        newlyEmbedded.Add(batch[j]);
                    }
                }

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Embedded {Done}/{Total}", Math.Min(i + BatchSize, unembedded.Count), unembedded.Count);
            }
        }

        var reclassified = await ClassifyAsync(db, newlyEmbedded, ct);
        return new EmbedRunResult(embedded, failed, reclassified);
    }

    // Only newly embedded events are classified — categories are deterministic
    // given the fixed category descriptions, so already-classified events never
    // change. Avoids reading the whole Events table on every run.
    private async Task<int> ClassifyAsync(AppDbContext db, List<Event> events, CancellationToken ct)
    {
        if (events.Count == 0) return 0;

        await classifier.InitializeAsync(ct);
        if (!classifier.IsReady)
        {
            logger.LogWarning("CategoryClassifier not ready (no API key?), skipping classification");
            return 0;
        }

        var updated = 0;
        foreach (var ev in events)
        {
            var newCategory = classifier.Classify(ev.Embedding!);
            if (newCategory == ev.Category) continue;
            ev.Category = newCategory;
            updated++;
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Classified {Updated}/{Total} newly embedded events", updated, events.Count);
        }

        return updated;
    }
}

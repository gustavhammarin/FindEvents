using App.Persistence;
using Microsoft.EntityFrameworkCore;

namespace App.Embedder;

public class EventEmbeddingService(
    IServiceScopeFactory scopeFactory,
    CategoryClassifierService classifier,
    ILogger<EventEmbeddingService> logger)
{
    private const int BatchSize = 50;
    private readonly SemaphoreSlim _runLock = new(1, 1);

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!await _runLock.WaitAsync(0))
        {
            logger.LogDebug("Embedding backfill already running — skipping");
            return;
        }
        try { await BackfillAsync(ct); }
        finally { _runLock.Release(); }
    }

    private async Task BackfillAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var embedder = scope.ServiceProvider.GetRequiredService<MistralEmbeddingService>();

        var unembedded = await db.Events
            .Where(e => e.Embedding == null)
            .ToListAsync(ct);

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
                    batch[j].Embedding = vectors[j];

                await db.SaveChangesAsync(ct);
                logger.LogInformation("Embedded {Done}/{Total}", Math.Min(i + BatchSize, unembedded.Count), unembedded.Count);

                if (i + BatchSize < unembedded.Count)
                    await Task.Delay(1100, ct);
            }
        }

        await ClassifyAllAsync(db, ct);
    }

    private async Task ClassifyAllAsync(AppDbContext db, CancellationToken ct)
    {
        await classifier.InitializeAsync(ct);
        if (!classifier.IsReady)
        {
            logger.LogWarning("CategoryClassifier not ready (no API key?), skipping classification");
            return;
        }

        db.ChangeTracker.Clear();

        var events = await db.Events
            .Where(e => e.Embedding != null)
            .ToListAsync(ct);

        if (events.Count == 0) return;

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
            logger.LogInformation("Classified {Updated}/{Total} events by embedding", updated, events.Count);
        }
        else
        {
            logger.LogDebug("All {Total} events already have correct embedding-based categories", events.Count);
        }
    }
}

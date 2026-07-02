using App.Persistence;
using App.Scraper.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;

public class AppEventRepository(AppDbContext context) : IEventRepository
{
    public async Task<int> SaveEventsAsync(IEnumerable<EventInfo> events, CancellationToken ct)
    {
        var eventList = events
            .Where(e => !string.IsNullOrEmpty(e.Link))
            .GroupBy(e => e.Link)
            .Select(g => g.First())
            .ToList();

        var links = eventList.Select(e => e.Link).ToList();

        var existingLinks = await context.Events
            .Where(e => links.Contains(e.Link))
            .Select(e => e.Link)
            .ToHashSetAsync(ct);

        var newEvents = eventList
            .Where(e => !existingLinks.Contains(e.Link))
            .Select(MapToEvent)
            .ToList();

        if (newEvents.Count == 0) return 0;

        try
        {
            await context.Events.AddRangeAsync(newEvents, ct);
            await context.SaveChangesAsync(ct);
            return newEvents.Count;
        }
        catch (DbUpdateException)
        {
            // Unique Link index violation — another source saved the same link
            // concurrently. Retry one by one so the rest of the batch survives.
            context.ChangeTracker.Clear();
            var saved = 0;
            foreach (var ev in newEvents)
            {
                try
                {
                    context.Events.Add(ev);
                    await context.SaveChangesAsync(ct);
                    saved++;
                }
                catch (DbUpdateException)
                {
                    context.ChangeTracker.Clear();
                }
            }
            return saved;
        }
    }

    public async Task<HashSet<string>> GetExistingLinksAsync(IEnumerable<string> links)
    {
        var list = links.ToList();
        return await context.Events
            .Where(e => list.Contains(e.Link))
            .Select(e => e.Link)
            .ToHashSetAsync();
    }

    public async Task<HashSet<string>> GetLinksBySourceAsync(string source) =>
        await context.Events
            .Where(e => e.Source == source)
            .Select(e => e.Link)
            .ToHashSetAsync();

    public async Task<int> DeleteOldEventsAsync(DateOnly cutoff)
    {
        return await context.Events
            .Where(e => e.StartDate != null && e.StartDate < cutoff)
            .ExecuteDeleteAsync();
    }

    private static Event MapToEvent(EventInfo e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        ImageUrl = e.ImageUrl,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        StartTime = e.StartTime,
        EndTime = e.EndTime,
        Location = e.Location ?? "",
        Municipality = e.Municipality,
        Link = e.Link,
        Source = e.Source,
        Category = e.Category,
        Description = e.Description,
        Place = e.Place
    };
}

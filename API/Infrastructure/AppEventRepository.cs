using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Infrastructure;

public class AppEventRepository(AppDbContext context) : IEventRepository
{
    public async Task SaveEventsAsync(IEnumerable<EventInfo> events)
    {
        var eventList = events
            .Where(e => !string.IsNullOrEmpty(e.Link))
            .ToList();

        var links = eventList.Select(e => e.Link).ToList();

        var existingLinks = await context.Events
            .Where(e => links.Contains(e.Link))
            .Select(e => e.Link)
            .ToHashSetAsync();

        var newEvents = eventList
            .Where(e => !existingLinks.Contains(e.Link))
            .Select(MapToEvent)
            .ToList();

        if (newEvents.Count == 0) return;

        await context.Events.AddRangeAsync(newEvents);
        await context.SaveChangesAsync();
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

using Domain;
using EventScraper.Interfaces;
using EventScraper.models;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Infrastructure.Events;

public class AppEventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public AppEventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SaveEventsAsync(IEnumerable<EventInfo> events)
    {
        var eventList = events
            .Where(e => !string.IsNullOrEmpty(e.Link))
            .ToList();

        var links = eventList.Select(e => e.Link).ToList();

        var existingLinks = await _context.Events
            .Where(e => links.Contains(e.Link))
            .Select(e => e.Link)
            .ToHashSetAsync();

        var newEvents = eventList
            .Where(e => !existingLinks.Contains(e.Link))
            .Select(MapToEvent)
            .ToList();

        if (newEvents.Count == 0) return;

        await _context.Events.AddRangeAsync(newEvents);
        await _context.SaveChangesAsync();
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
        Description = e.Description
    };
}

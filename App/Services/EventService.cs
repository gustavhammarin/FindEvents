using EventScraper.Categorization;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace App.Services;

public class EventService(AppDbContext db, ILogger<EventService> logger)
{
    private const int MaxPageSize = 200;

    public async Task<PagedList<MinimalEventDto, EventCursor?>> GetEventsAsync(
        EventsFilter filter, CancellationToken ct = default)
    {
        try
        {
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

            var startDate = DateOnly.TryParse(filter.StartDate, out var sd)
                ? sd
                : DateOnly.FromDateTime(DateTime.UtcNow);

            var q = db.Events.AsNoTracking().Where(e => e.StartDate >= startDate);

            if (!string.IsNullOrEmpty(filter.CursorStartDate) &&
                !string.IsNullOrEmpty(filter.CursorId) &&
                DateOnly.TryParse(filter.CursorStartDate, out var cd))
            {
                q = q.Where(e => e.StartDate > cd ||
                                 (e.StartDate == cd && string.Compare(e.Id, filter.CursorId) > 0));
            }

            if (filter.Categories.Count > 0)
            {
                q = q.Where(e => filter.Categories.Contains(e.Category));
            }

            if (filter.Places.Count > 0)
            {
                q = q.Where(e => e.Place != null && filter.Places.Contains(e.Place));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                q = q.Where(e =>
                    EF.Functions.ILike(e.Title, $"%{search}%") ||
                    (e.Description != null && EF.Functions.ILike(e.Description, $"%{search}%")) ||
                    (e.Location != null && EF.Functions.ILike(e.Location, $"%{search}%")) ||
                    EF.Functions.ILike(e.Municipality, $"%{search}%") ||
                    EF.Functions.TrigramsSimilarity(e.Title, search) > 0.2);
            }

            var events = await q
                .OrderBy(e => e.StartDate)
                .ThenBy(e => e.Id)
                .Take(pageSize + 1)
                .Select(e => MinimalEventDto.FromEntity(e))
                .ToListAsync(ct);

            EventCursor? nextCursor = null;
            if (events.Count > pageSize)
            {
                var last = events[^1];
                nextCursor = new EventCursor { StartDate = last.StartDate ?? DateOnly.MinValue, Id = last.Id };
                events.RemoveAt(events.Count - 1);
            }

            return new PagedList<MinimalEventDto, EventCursor?> { Items = events, NextCursor = nextCursor };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetEventsAsync failed. Filter={@Filter}", filter);
            return new PagedList<MinimalEventDto, EventCursor?> { Items = [], NextCursor = null };
        }
    }

    public async Task<EventDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return ev is null ? null : EventDto.FromEntity(ev);
    }

    public async Task<FiltersDto> GetFiltersAsync(CancellationToken ct = default)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var places = await db.Events
                .AsNoTracking()
                .Where(e => e.StartDate >= today && e.Place != null && e.Place != "")
                .Select(e => e.Place!)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync(ct);

            return new FiltersDto(EventCategories.Categories, places);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetFiltersAsync failed");
            return new FiltersDto(EventCategories.Categories, []);
        }
    }
}
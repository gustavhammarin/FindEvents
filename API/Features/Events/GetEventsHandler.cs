using API.Core;
using EventScraper.Categorization;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Features.Events;

public class GetEventsHandler(AppDbContext db, ILogger<GetEventsHandler> logger)
{
    private const int MaxPageSize = 200;

    public async Task<Result<PagedList<MinimalEventDto, EventCursor?>>> HandleAsync(
        GetEventsQuery query,
        CancellationToken ct = default)
    {
        try
        {
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            var startDate = DateOnly.TryParse(query.StartDate, out var sd)
                ? sd
                : DateOnly.FromDateTime(DateTime.UtcNow);

            var q = db.Events
                .AsNoTracking()
                .Where(e => e.StartDate >= startDate);

            if (!string.IsNullOrEmpty(query.CursorStartDate) &&
                !string.IsNullOrEmpty(query.CursorId) &&
                DateOnly.TryParse(query.CursorStartDate, out var cursorDate))
            {
                q = q.Where(e => e.StartDate > cursorDate ||
                                 (e.StartDate == cursorDate && string.Compare(e.Id, query.CursorId) > 0));
            }

            if (!string.IsNullOrWhiteSpace(query.Municipality))
            {
                var muni = query.Municipality.Trim().ToLower();
                q = q.Where(e => e.Municipality.ToLower().Contains(muni));
            }

            if (!string.IsNullOrWhiteSpace(query.Category) &&
                EventCategories.Categories.Contains(query.Category))
                q = q.Where(e => e.Category == query.Category);

            if (!string.IsNullOrWhiteSpace(query.Source))
                q = q.Where(e => e.Source == query.Source.Trim());

            if (!string.IsNullOrWhiteSpace(query.Place))
            {
                var place = query.Place.Trim().ToLower();
                q = q.Where(e => e.Place != null && e.Place.ToLower() == place);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
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
                nextCursor = new EventCursor
                {
                    StartDate = last.StartDate ?? DateOnly.MinValue,
                    Id = last.Id
                };
                events.RemoveAt(events.Count - 1);
            }

            return new PagedList<MinimalEventDto, EventCursor?> { Items = events, NextCursor = nextCursor };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query events. Search={Search} Municipality={Municipality} Place={Place} Category={Category}",
                query.Search, query.Municipality, query.Place, query.Category);
            return EventErrors.QueryFailed;
        }
    }
}

using Application.Activities.Core;
using Application.Core;
using Application.Events.DTOs;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace API.Features.Events;

public class GetEventsHandler(AppDbContext db)
{
    private const int MaxPageSize = 50;

    private static readonly HashSet<string> ValidCategories =
    [
        "Musik & Konsert", "Teater & Show", "Konst & Utställning",
        "Föreläsning & Utbildning", "Workshop & Kurs", "Sport & Tävling",
        "Träning & Motion", "Natur & Friluftsliv", "Mat & Dryck",
        "Marknad & Loppis", "Familj & Barn", "Seniorer & Pensionärer",
        "Hälsa & Välmående", "Socialt & Träffpunkt", "Övrigt"
    ];

    public async Task<PagedList<EventDto, EventCursor?>> HandleAsync(
        GetEventsQuery query,
        CancellationToken ct = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var startDate = DateOnly.TryParse(query.StartDate, out var sd)
            ? sd
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var q = db.Events
            .AsNoTracking()
            .Where(e => e.StartDate >= startDate);

        // Cursor
        if (!string.IsNullOrEmpty(query.CursorStartDate) &&
            !string.IsNullOrEmpty(query.CursorId) &&
            DateOnly.TryParse(query.CursorStartDate, out var cursorDate))
        {
            q = q.Where(e => e.StartDate > cursorDate ||
                             (e.StartDate == cursorDate && string.Compare(e.Id, query.CursorId) > 0));
        }

        // Municipality filter
        if (!string.IsNullOrWhiteSpace(query.Municipality))
        {
            var muni = query.Municipality.Trim().ToLower();
            q = q.Where(e => e.Municipality.ToLower().Contains(muni));
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(query.Category) && ValidCategories.Contains(query.Category))
            q = q.Where(e => e.Category == query.Category);

        // Source filter
        if (!string.IsNullOrWhiteSpace(query.Source))
            q = q.Where(e => e.Source == query.Source.Trim());

        // Text search — ILike (case-insensitive) + trigram similarity for fuzzy tolerance
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
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                ImageUrl = e.ImageUrl,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Location = e.Location,
                Link = e.Link,
                Source = e.Source,
                Municipality = e.Municipality,
                Category = e.Category,
                Description = e.Description
            })
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

        return new PagedList<EventDto, EventCursor?> { Items = events, NextCursor = nextCursor };
    }
}

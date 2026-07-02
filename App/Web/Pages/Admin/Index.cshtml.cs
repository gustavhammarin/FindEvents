using App.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Pages.Admin;

public class IndexModel(AppDbContext db) : PageModel
{
    public int TotalEvents { get; private set; }
    public int UpcomingEvents { get; private set; }
    public int UnembeddedEvents { get; private set; }
    public List<SourceCount> EventsBySource { get; private set; } = [];
    public List<ScrapeRun> Runs { get; private set; } = [];

    public record SourceCount(string Source, int Count);

    public async Task OnGetAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        TotalEvents = await db.Events.CountAsync();
        UpcomingEvents = await db.Events.CountAsync(e => e.StartDate >= today);
        UnembeddedEvents = await db.Events.CountAsync(e => e.Embedding == null);

        EventsBySource = (await db.Events
                .GroupBy(e => e.Source)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .ToListAsync())
            .Select(s => new SourceCount(s.Source, s.Count))
            .ToList();

        Runs = await db.ScrapeRuns
            .AsNoTracking()
            .Include(r => r.Sources)
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(20)
            .ToListAsync();
    }
}

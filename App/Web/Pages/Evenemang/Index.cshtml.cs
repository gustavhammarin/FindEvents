using App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace App.Web.Pages.Evenemang;

public class IndexModel(EventService eventService) : PageModel
{
    public List<MinimalEventDto> Events { get; private set; } = [];
    public EventCursor? NextCursor { get; private set; }
    public List<string> AllCategories { get; private set; } = [];
    public List<string> AllPlaces { get; private set; } = [];
    public int Take { get; private set; } = 32;
    public string Q { get; private set; } = "";
    public List<string> SelectedCategories { get; private set; } = [];
    public List<string> SelectedPlaces { get; private set; } = [];
    public string Datum { get; private set; } = "";

    public async Task OnGetAsync(
        string? q,
        [FromQuery(Name = "cat")] List<string>? cat,
        [FromQuery(Name = "plats")] List<string>? plats,
        string? datum,
        int ta = 32)
    {
        Q = q ?? "";
        SelectedCategories = cat ?? [];
        SelectedPlaces = plats ?? [];
        Datum = datum ?? "";
        Take = Math.Clamp(ta, 32, 200);

        var filters = await eventService.GetFiltersAsync();
        AllCategories = filters.Categories.ToList();
        AllPlaces = filters.Places.ToList();

        var page = await eventService.GetEventsAsync(new EventsFilter
        {
            Search = NullIfEmpty(q),
            Categories = SelectedCategories,
            Places = SelectedPlaces,
            StartDate = NullIfEmpty(datum),
            PageSize = Take,
        });

        Events = page.Items;
        NextCursor = page.NextCursor;
    }

    public async Task<IActionResult> OnGetCardsAsync(
        string? q,
        [FromQuery(Name = "cat")] List<string>? cat,
        [FromQuery(Name = "plats")] List<string>? plats,
        string? datum,
        int ta = 32)
    {
        var take = Math.Clamp(ta, 32, 200);
        var page = await eventService.GetEventsAsync(new EventsFilter
        {
            Search = NullIfEmpty(q),
            Categories = cat ?? [],
            Places = plats ?? [],
            StartDate = NullIfEmpty(datum),
            PageSize = take,
        });

        return Partial("_EventCards", new EventCardsViewModel(page.Items, page.NextCursor, take));
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

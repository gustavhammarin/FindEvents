using App.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace App.Web.Pages.Evenemang;

public class DetailModel(EventService eventService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = "";

    public EventDto? Event { get; private set; }
    public List<MinimalEventDto> Similar { get; private set; } = [];
    public string CanonicalUrl { get; private set; } = "";
    public string? Description { get; private set; }
    public string? DateLabel { get; private set; }
    public string? TimeLabel { get; private set; }
    public string? LocationText { get; private set; }
    public string ExternalLink { get; private set; } = "#";
    public bool HasImage { get; private set; }
    public string AbsoluteImageUrl { get; private set; } = "";
    public string JsonLd { get; private set; } = "{}";

    public async Task<IActionResult> OnGetAsync()
    {
        Event = await eventService.GetByIdAsync(Id);
        if (Event is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Page();
        }

        CanonicalUrl = $"{Request.Scheme}://{Request.Host}/evenemang/{Id}";
        Similar = await eventService.GetSimilarEventsAsync(Id, count: 6);

        HasImage = !string.IsNullOrEmpty(Event.ImageUrl)
            && !Event.ImageUrl.Contains("placeholder")
            && Event.ImageUrl.StartsWith("http");
        if (HasImage)
            AbsoluteImageUrl = $"{Request.Scheme}://{Request.Host}/img/{Id}";

        Description = string.IsNullOrEmpty(Event.Description)
            ? null
            : Event.Description.Length > 160
                ? Event.Description[..160] + "…"
                : Event.Description;

        var svSE = new System.Globalization.CultureInfo("sv-SE");
        if (Event.StartDate.HasValue)
        {
            var startStr = Event.StartDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dddd d MMMM yyyy", svSE);
            DateLabel = Event.EndDate.HasValue && Event.EndDate != Event.StartDate
                ? $"{startStr} – {Event.EndDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dddd d MMMM yyyy", svSE)}"
                : startStr;
        }

        if (Event.StartTime.HasValue)
        {
            var startT = Event.StartTime.Value.ToString("HH:mm");
            TimeLabel = Event.EndTime.HasValue
                ? $"{startT} – {Event.EndTime.Value.ToString("HH:mm")}"
                : startT;
        }

        LocationText = Event.Location is not null && Event.Location != Event.Municipality
            ? $"{Event.Location}, {Event.Municipality}"
            : Event.Municipality;

        ExternalLink = Event.Link.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? Event.Link
            : $"https://jkpg.com{Event.Link}";

        JsonLd = BuildJsonLd();
        return Page();
    }

    private string BuildJsonLd()
    {
        if (Event is null) return "{}";

        var startIso = Event.StartDate.HasValue
            ? Event.StartDate.Value.ToDateTime(Event.StartTime ?? TimeOnly.MinValue)
                .ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture)
            : null;
        var endIso = Event.EndDate.HasValue
            ? Event.EndDate.Value.ToDateTime(Event.EndTime ?? TimeOnly.MinValue)
                .ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture)
            : null;

        var name = JsonEscape(Event.Title);
        var desc = JsonEscape(Description ?? "");
        var loc = JsonEscape(Event.Location ?? Event.Municipality ?? "");
        var municipality = JsonEscape(Event.Municipality ?? "Jönköping");
        var url = JsonEscape(CanonicalUrl);
        var img = HasImage ? JsonEscape(AbsoluteImageUrl) : "";

        var sb = new System.Text.StringBuilder();
        sb.Append("{\"@context\":\"https://schema.org\",\"@type\":\"Event\"");
        sb.Append($",\"name\":\"{name}\"");
        if (startIso != null) sb.Append($",\"startDate\":\"{startIso}\"");
        if (endIso != null) sb.Append($",\"endDate\":\"{endIso}\"");
        if (!string.IsNullOrEmpty(desc)) sb.Append($",\"description\":\"{desc}\"");
        if (!string.IsNullOrEmpty(img)) sb.Append($",\"image\":\"{img}\"");
        sb.Append($",\"url\":\"{url}\"");
        sb.Append($",\"location\":{{\"@type\":\"Place\",\"name\":\"{(string.IsNullOrEmpty(loc) ? municipality : loc)}\",\"address\":{{\"@type\":\"PostalAddress\",\"addressLocality\":\"{municipality}\",\"addressRegion\":\"Jönköpings län\",\"addressCountry\":\"SE\"}}}}");
        sb.Append(",\"eventStatus\":\"https://schema.org/EventScheduled\"");
        sb.Append(",\"eventAttendanceMode\":\"https://schema.org/OfflineEventAttendanceMode\"");
        if (!string.IsNullOrEmpty(Event.Category)) sb.Append($",\"keywords\":\"{JsonEscape(Event.Category)}\"");
        sb.Append(",\"organizer\":{\"@type\":\"Organization\",\"name\":\"Hitta Evenemang\",\"url\":\"https://hittaevenemang.se\"}");
        sb.Append('}');
        return sb.ToString();
    }

    private static string JsonEscape(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"")
         .Replace("\n", "\\n").Replace("\r", "").Replace("\t", "\\t");
}

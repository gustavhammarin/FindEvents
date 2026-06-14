using Microsoft.AspNetCore.Mvc;

namespace API.Features.Events;

public class GetEventsQuery
{
    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "municipality")]
    public string? Municipality { get; set; }

    [FromQuery(Name = "category")]
    public string? Category { get; set; }

    [FromQuery(Name = "cursorStartDate")]
    public string? CursorStartDate { get; set; }

    [FromQuery(Name = "cursorId")]
    public string? CursorId { get; set; }

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 16;

    [FromQuery(Name = "startDate")]
    public string? StartDate { get; set; }

    [FromQuery(Name = "source")]
    public string? Source { get; set; }
}

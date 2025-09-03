using System;
using Application.Activities.Core;
using Application.Core;

namespace Application.Events.Queries;

public class EventParams : PaginationParams<EventCursor?>
{
    public string? Filter { get; set; }
    public string? Search { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

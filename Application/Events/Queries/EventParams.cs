using System;
using Application.Activities.Core;

namespace Application.Events.Queries;

public class EventParams : PaginationParams<DateTime?>
{
    public string? Filter { get; set; }
    public string? Search { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
}

using System;
using Microsoft.EntityFrameworkCore;

namespace Domain;

[Index(nameof(StartDate))]
public class Event
{

    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Location { get; set; }
    public required string Municipality { get; set; }
    public required string Link { get; set; }
    public required string Source { get; set; }
    public required string Category { get; set; }
}

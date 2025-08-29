using System;

namespace EventScraper.models;

public class EventInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public string Link { get; set; } = "";
    public string Source { get; set; } = "";
    public string Municipality { get; set; } = "";
    public string Category { get; set; } = "";
}

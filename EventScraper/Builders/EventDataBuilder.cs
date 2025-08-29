using System;
using EventScraper.models;

namespace EventScraper.Builders;

public class EventDataBuilder
{
    public DateOnly? StartDate { get; set; }
    public string ImageUrl { get; set; } = "";
    public DateOnly? EndDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public string Link { get; set; } = "";
    public string Source { get; set; } = "";
    public string Municipality { get; set; } = "";


    public EventInfo Build(string title)
    {
        return new EventInfo
        {
            Title = title,
            ImageUrl = ImageUrl,
            StartDate = StartDate,
            EndDate = EndDate,
            StartTime = StartTime,
            EndTime = EndTime,
            Location = Location,
            Description = Description,
            Link = Link,
            Municipality = Municipality,
            Source = Source
        };
    }
}


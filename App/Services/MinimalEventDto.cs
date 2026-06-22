using App.Persistence;

namespace App.Services;

public record MinimalEventDto(
    string Id,
    string Title,
    string? ImageUrl,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Location,
    string Municipality,
    string? Place,
    string Link,
    string Source,
    string Category)
{
    public static MinimalEventDto FromEntity(Event e) => new(
        e.Id, e.Title, e.ImageUrl,
        e.StartDate, e.EndDate, e.StartTime, e.EndTime,
        e.Location, e.Municipality, e.Place, e.Link, e.Source, e.Category);
}

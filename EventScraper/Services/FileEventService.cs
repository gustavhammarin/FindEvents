using EventScraper.Interfaces;
using EventScraper.models;
using Newtonsoft.Json;

public class FileEventRepository : IFileEventRepository
{
    private readonly string _filePath = "events.json";

    public async Task SaveEventsAsync(IEnumerable<EventInfo> events)
    {
        var existingEvents = await GetEventsAsync();
        var allEvents = existingEvents.Concat(events).ToList();

        var json = JsonConvert.SerializeObject(allEvents, Formatting.Indented);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<IEnumerable<EventInfo>> GetEventsAsync(DateOnly? from = null, DateOnly? to = null)
    {
        if (!File.Exists(_filePath))
            return Enumerable.Empty<EventInfo>();

        var json = await File.ReadAllTextAsync(_filePath);
        var events = JsonConvert.DeserializeObject<List<EventInfo>>(json) ?? new();

        return events.Where(e =>
            (!from.HasValue || e.StartDate >= from.Value) &&
            (!to.HasValue || e.StartDate <= to.Value));
    }

    public async Task<bool> EventExistsAsync(string title, DateOnly? startDate, string location)
    {
        var events = await GetEventsAsync();
        return events.Any(e =>
            e.Title == title &&
            (!startDate.HasValue || e.StartDate == startDate.Value) &&
            e.Location == location);
    }
}
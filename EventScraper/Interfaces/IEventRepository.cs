using EventScraper.models;

namespace EventScraper.Interfaces;

public interface IEventRepository
{
    Task SaveEventsAsync(IEnumerable<EventInfo> events);
}

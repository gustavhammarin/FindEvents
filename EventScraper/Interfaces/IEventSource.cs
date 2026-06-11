using EventScraper.models;

namespace EventScraper.Interfaces;

public interface IEventSource
{
    string Name { get; }
    Task<IEnumerable<EventInfo>> FetchAsync(CancellationToken ct = default);
}

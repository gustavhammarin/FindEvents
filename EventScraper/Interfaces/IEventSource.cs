using EventScraper.models;

namespace EventScraper.Interfaces;

public interface IEventSource
{
    string Name { get; }
    IAsyncEnumerable<EventInfo> FetchAsync(CancellationToken ct = default);
}

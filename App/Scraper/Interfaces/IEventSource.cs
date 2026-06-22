using App.Scraper.Models;

namespace App.Scraper.Interfaces;

public interface IEventSource
{
    string Name { get; }
    IAsyncEnumerable<EventInfo> FetchAsync(CancellationToken ct = default);
}

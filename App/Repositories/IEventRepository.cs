using App.Scraper.Models;

namespace App.Repositories;

public interface IEventRepository
{
    Task SaveEventsAsync(IEnumerable<EventInfo> events, CancellationToken ct);
    Task<HashSet<string>> GetExistingLinksAsync(IEnumerable<string> links);
    Task<HashSet<string>> GetLinksBySourceAsync(string source);
    Task<int> DeleteOldEventsAsync(DateOnly cutoff);
}

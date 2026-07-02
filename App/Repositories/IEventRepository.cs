using App.Scraper.Models;

namespace App.Repositories;

public interface IEventRepository
{
    /// <summary>Saves new events (existing links are skipped). Returns the number of events actually inserted.</summary>
    Task<int> SaveEventsAsync(IEnumerable<EventInfo> events, CancellationToken ct);
    Task<HashSet<string>> GetExistingLinksAsync(IEnumerable<string> links);
    Task<HashSet<string>> GetLinksBySourceAsync(string source);
    Task<int> DeleteOldEventsAsync(DateOnly cutoff);
}

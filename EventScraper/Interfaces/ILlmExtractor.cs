using EventScraper.models;

namespace EventScraper.Interfaces;

public interface ILlmExtractor
{
    Task<EventInfo?> ExtractAsync(string text, string sourceUrl, string municipality, CancellationToken ct = default);
}

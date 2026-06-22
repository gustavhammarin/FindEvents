using App.Scraper.Models;

namespace App.Scraper.Interfaces;

public interface ILlmExtractor
{
    Task<EventInfo?> ExtractAsync(string text, string sourceUrl, string municipality, CancellationToken ct = default);
}

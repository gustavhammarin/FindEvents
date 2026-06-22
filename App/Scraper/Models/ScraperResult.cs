namespace App.Scraper.Models;

public class ScraperResult
{
    public string? ScraperName { get; set; }
    public bool Success { get; set; }
    public int EventCount { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<EventInfo> Events { get; set; } = new List<EventInfo>();
}

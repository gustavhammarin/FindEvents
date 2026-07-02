namespace App.Scraper.Models;

public class ScraperResult
{
    public string ScraperName { get; set; } = "";
    public bool Success { get; set; }
    public int EventsFetched { get; set; }
    public int EventsSaved { get; set; }
    public double DurationSeconds { get; set; }
    public string? ErrorMessage { get; set; }
}

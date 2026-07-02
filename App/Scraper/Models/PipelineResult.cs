namespace App.Scraper.Models;

public class PipelineResult
{
    public int TotalScrapers { get; set; }
    public int SuccessfulScrapers { get; set; }
    public int TotalEventsFetched { get; set; }
    public int TotalEventsSaved { get; set; }
    public int EventsDeleted { get; set; }
    public List<ScraperResult> ScraperResults { get; set; } = [];
}

using System;

namespace EventScraper.models;

public class PipelineResult
{
    public int TotalScrapers { get; set; }
    public int SuccessfulScrapers { get; set; }
    public int TotalEvents { get; set; }
    public IEnumerable<ScraperResult>? ScraperResults { get; set; }
}

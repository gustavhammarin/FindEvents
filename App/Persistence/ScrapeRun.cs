using Microsoft.EntityFrameworkCore;

namespace App.Persistence;

[Index(nameof(StartedAtUtc))]
public class ScrapeRun
{
    public int Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    /// <summary>"scheduled" (recurring pipeline run) or "startup" (embed backfill at app start).</summary>
    public string Trigger { get; set; } = "scheduled";

    public int TotalSources { get; set; }
    public int SuccessfulSources { get; set; }
    public int EventsFetched { get; set; }
    public int EventsSaved { get; set; }
    public int EventsDeleted { get; set; }
    public int EventsEmbedded { get; set; }
    public int EmbeddingFailures { get; set; }
    public int EventsReclassified { get; set; }
    public string? Error { get; set; }

    public List<ScrapeRunSource> Sources { get; set; } = [];
}

public class ScrapeRunSource
{
    public int Id { get; set; }
    public int ScrapeRunId { get; set; }
    public ScrapeRun ScrapeRun { get; set; } = null!;

    public string SourceName { get; set; } = "";
    public bool Success { get; set; }
    public int EventsFetched { get; set; }
    public int EventsSaved { get; set; }
    public double DurationSeconds { get; set; }
    public string? Error { get; set; }
}

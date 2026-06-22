using App.Repositories;
using App.Scraper.Interfaces;

namespace App.Scraper.Sources;

// Municipality sources that share the LLM extraction flow.
// Discovery differs per site (sitemap or list page), extraction is identical.

public class MullsjoSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<MullsjoSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "mullsjo.se";
    protected override string Municipality => "Mullsjö";
    protected override string BaseUrl => "https://www.mullsjo.se";

    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromSitemapAsync("https://www.mullsjo.se/sitemap1.xml.gz",
            u => u.Contains("/arkiv/evenemang/evenemang/"));
}


public class GislavedSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<GislavedSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "gislaved.se";
    protected override string Municipality => "Gislaved";
    protected override string BaseUrl => "https://www.gislaved.se";

    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromSitemapAsync("https://www.gislaved.se/sitemap1.xml.gz",
            u => u.Contains("/evenemangskalender/evenemang"));
}


public class AnebySource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<AnebySource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "aneby.se";
    protected override string Municipality => "Aneby";
    protected override string BaseUrl => "https://www.aneby.se";

    // No sitemap on aneby.se — the archive page lists all upcoming events.
    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromListPageAsync("https://www.aneby.se/arkiv/evenemang.html",
            @"/arkiv/evenemang/evenemang/[^""]+");
}

public class GnosjoSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<GnosjoSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "gnosjoandan.com";
    protected override string Municipality => "Gnosjö";
    protected override string BaseUrl => "https://www.gnosjoandan.com";

    protected override async Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct)
    {
        var all = new List<string>();
        for (var page = 1; page <= 10; page++)
        {
            var url = $"https://www.gnosjoandan.com/visit-gnosjo/evenemang?page={page}";
            var batch = (await FromListPageAsync(url, @"/sv/visit-gnosjo/evenemang/[^""]+")).ToList();
            if (batch.Count == 0) break;
            all.AddRange(batch);
            if (batch.Count < 9) break; // last page has fewer than full page
        }
        return all;
    }
}

public class VaggerydSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<VaggerydSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "vaggeryd.se";
    protected override string Municipality => "Vaggeryd";
    protected override string BaseUrl => "https://www.vaggeryd.se";

    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromSitemapAsync("https://www.vaggeryd.se/sitemap1.xml.gz",
            u => u.Contains("/uppleva-och-gora/uplev-vaggeryds--kommun/") ||
                 u.Contains("/uppleva-och-gora/upplev-vaggeryds--kommun/"));
}

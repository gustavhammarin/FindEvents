using EventScraper.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventScraper.Sources;

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

public class SavsjoSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<SavsjoSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "savsjo.se";
    protected override string Municipality => "Sävsjö";
    protected override string BaseUrl => "https://www.savsjo.se";

    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromSitemapAsync("https://www.savsjo.se/sitemap1.xml.gz",
            u => u.Contains("/nyheter/evenemang/"));
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

public class EksjoSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<EksjoSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "visiteksjo.se";
    protected override string Municipality => "Eksjö";
    protected override string BaseUrl => "https://visiteksjo.se";

    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromSitemapAsync("https://visiteksjo.se/sitemap1.xml.gz",
            u => u.Contains("/artikelarkiv/evenemang/"));
}

public class VetlandaSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<VetlandaSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "vetlanda.se";
    protected override string Municipality => "Vetlanda";
    protected override string BaseUrl => "https://www.vetlanda.se";

    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromSitemapAsync("https://www.vetlanda.se/sitemap1.xml.gz",
            u => u.Contains("/evenemangskalender/evenemang/"));
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

    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromListPageAsync("https://www.gnosjoandan.com/visit-gnosjo/evenemang",
            @"/sv/visit-gnosjo/evenemang/[^""]+");
}

public class VaggerydSource(IHttpLoader loader, ILlmExtractor llm, IEventRepository repo, ILogger<VaggerydSource> logger)
    : LlmHtmlSource(loader, llm, repo, logger)
{
    public override string Name => "vaggeryd.se";
    protected override string Municipality => "Vaggeryd";
    protected override string BaseUrl => "https://www.vaggeryd.se";

    // Vaggeryd has no public event calendar; the business calendar is the only events feed.
    protected override Task<IEnumerable<string>> DiscoverUrlsAsync(CancellationToken ct) =>
        FromListPageAsync("https://www.vaggeryd.se/naringsliv-och-arbete/kalender-for-naringslivet.html",
            @"/naringsliv-och-arbete/kalender-for-naringslivet/kalender-for-naringslivet/[^""]+");
}

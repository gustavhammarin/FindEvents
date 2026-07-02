# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Start dev infra (postgres + pgadmin)
docker compose up postgres -d

# Run app (auto-migrates on startup, starts scraper background service)
dotnet run --project App

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project App

# Build solution
dotnet build FindEvents.sln

# Run tests (xunit)
dotnet test Tests/Tests.csproj

# Production deploy (app + postgres + Caddy with auto-HTTPS)
docker compose -f docker-compose.prod.yaml up -d --build
```

## Configuration

Single gitignored `.env` at repo root holds ALL secrets and environment config — loaded by DotNetEnv in `Program.cs` (and `AppDbContextFactory` for design-time EF). Template: `.env.example`. App settings use `Section__Key` env var form. Non-secret defaults: `App/appsettings.json`.

Key vars: `ConnectionStrings__DefaultConnection`, `MistralSettings__ApiKey`, `Scraper__Enabled`, `Admin__Password` (empty = /admin returns 404), `DOMAIN` (Caddy).

## Architecture

Single ASP.NET Core project (`App/`), .NET 10. HTMX + Razor Pages frontend, no API controllers, no separate frontend build.

```
App/Program.cs           → DI wiring, minimal endpoints (robots.txt, sitemap.xml), auto-migration
App/Persistence/         → EF Core AppDbContext + migrations; Event, ScrapeRun/ScrapeRunSource entities
App/Repositories/        → IEventRepository (SaveEventsAsync returns inserted count; dedupes by unique Link)
App/Services/            → EventService: cursor pagination, trigram + semantic search, similar events
App/Embedder/            → MistralEmbeddingService (+retry/rate limit), EventEmbeddingService backfill,
                           CategoryClassifierService (embedding cosine → 15 categories)
App/Scraper/             → Sources + LLM extraction + categorization + pipeline
App/Web/Pages/           → Razor Pages (pages root = /Web/Pages)
  Evenemang/             → /evenemang (filter grid + handler=Cards partial), /evenemang/{id}
  Admin/                 → /admin: scrape/embed run history + DB stats (HTTP Basic Auth, user "admin")
App/wwwroot/             → css/app.css, js/htmx-filters.js, js/htmx.min.js (self-hosted)
Dockerfile               → multi-stage publish; docker-compose.prod.yaml adds postgres + Caddy
```

### Scraper pipeline
All sources implement `IEventSource` (`Name` + `FetchAsync`), registered in `ScraperServiceExtensions`. `ScraperPipeline.RunAllAsync()` runs all sources with `SemaphoreSlim(5)`, dedupes by `(Title.lower, StartDate)`, backfills `Category`, saves via `IEventRepository`, deletes past events. `ScraperHostedService` runs every 24h (`Scraper__Enabled=false` disables), records every run to the `ScrapeRuns` table (per-source counts, embedding stats, errors) — shown on /admin.

Two source kinds in `App/Scraper/Sources/`:
- **Structured** (no LLM): `JkpgSource`, `HaboSource`, `VarnamoSource` (Cruncho), `SvSource`, `TranasSource` (WP REST)
- **LLM-based**: subclass `LlmHtmlSource` — discovers URLs (`FromSitemapAsync`/`FromListPageAsync`), skips links already in DB before any LLM call, strips HTML, sends text to `ILlmExtractor`. Small ones live in `LlmSources.cs`

### LLM extraction & resilience
`MistralExtractor` (`ILlmExtractor`) is used when `MistralSettings__ApiKey` is set; otherwise `MlxExtractor` (local oMLX, OpenAI-compatible). All Mistral calls (extraction + embeddings) go through the singleton `MistralRateLimiter` (~1 req/s, free tier) and retry 429/5xx/timeouts with Retry-After-aware backoff. `HttpLoader` retries transient fetch failures. Failed extractions are self-healing: the event's link never reaches the DB, so the next run retries it.

### Embeddings & categorization
After each scrape (and at startup), `EventEmbeddingService.RunAsync()` embeds events with `Embedding == null` and classifies only those newly embedded events via `CategoryClassifierService` (cosine distance to 15 fixed Swedish category description embeddings). Nothing new → zero API calls and zero DB scans. If category descriptions ever change, force a full reclassification with `UPDATE "Events" SET "Embedding" = NULL` (they'll be re-embedded and re-classified next run). Keyword fallback: `EventCategorizer.Categorize()` in `App/Scraper/Categorization/` (tests in `Tests/Events/EventCategorizerTests.cs`).

### Search
`EventService.GetEventsAsync`: SQL ILIKE + pg_trgm trigram similarity. Semantic similar-events via pgvector cosine on the detail page. No Elasticsearch.

### Frontend (HTMX + Razor Pages)
Filter bar: `<form id="filters">` with `hx-get="/evenemang?handler=Cards"` on `change`/`keyup delay:350ms`; response replaces `#events-container` with `_EventCards.cshtml` partial (OOB swap for `#event-count`). Load more: button re-requests with `ta=N+32`, full replace. Custom dropdowns/datepicker: vanilla JS in `htmx-filters.js`. URL sync via `history.replaceState` on `htmx:afterSettle`.

### SEO
`_Layout.cshtml` owns `<title>` (from `ViewData["Title"]`). Index: canonical, OG/Twitter meta, ItemList + WebSite JSON-LD, `noindex` when filters active. Detail: schema.org Event JSON-LD, OG image, 404 status for missing events. `robots.txt` + `sitemap.xml` are minimal endpoints in `Program.cs`. `UseForwardedHeaders` trusts Caddy's X-Forwarded-Proto/Host so generated URLs are https.

### Adding a scraper source
LLM site: subclass `LlmHtmlSource` (implement `Name`, `Municipality`, `BaseUrl`, `DiscoverUrlsAsync`). Structured site: implement `IEventSource`. Register in `ScraperServiceExtensions.AddScraperServices`.

`Domain.Event.Link` has a unique index — dedup relies on this at the DB level; `SaveEventsAsync` falls back to row-by-row insert on unique violations.

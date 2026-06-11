# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Backend
```bash
# Start infra (postgres only)
docker compose up postgres -d

# Start all infra (postgres + elasticsearch + kibana)
docker compose up -d

# Run API (auto-migrates on startup, starts scraper background service)
dotnet run --project API

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project Persistence --startup-project API

# Build solution
dotnet build FindEvents.sln

# Run tests (xunit)
dotnet test Tests/Tests.csproj
```

### Frontend
```bash
cd client
npm install
npm run dev      # http://localhost:5173
npm run build
npm run lint
```

### EventTrainer (dev utility)
```bash
dotnet run --project EventTrainer   # one-off keyword backfill of Category in DB (+ legacy ML.NET experiment)
```

## Architecture

**Clean Architecture**, vertical slice features in API (MediatR removed):

```
Domain          → Entity models (Event)
Persistence     → EF Core DbContext + migrations (AppDbContext)
Application     → Shared core (Result, PagedList, EventCursor, EventDto, IElasticService)
Infrastructure  → IEventRepository impl (AppEventRepository)
API             → Features (vertical slices), modules, middleware, DI wiring, ElasticService
EventScraper    → Background scraping library (sources + LLM extraction + categorization)
```

### Request flow
Minimal API endpoints, no controllers for features. `API/Features/Events/EventsModule.cs` maps `GET /api/events` → `GetEventsHandler.HandleAsync(GetEventsQuery)` → `PagedList<EventDto, EventCursor?>`.

**Module pattern**: implement `API/Modules/IModule` (`RegisterServices` + `MapEndpoints`) — auto-discovered and wired in `Program.cs` via reflection. New feature = new folder under `API/Features/` with Module + Query + Handler.

### Pagination
Cursor-based (`EventCursor { StartDate, Id }`). Query params: `cursorStartDate`, `cursorId`, `pageSize`, `search`, `municipality`, `category`, `startDate`. Default page size 16, max 50.

### Search
`ElasticService.SearchQuery()` returns matching event IDs → EF `WHERE id IN (...)`. Falls back to SQL `LIKE` when Elasticsearch is unreachable or returns no results. Elasticsearch is optional — `_client` is null when unconfigured.

### Scraper system
All sources implement `IEventSource` (`Name` + `FetchAsync`) — auto-registered in `Program.cs` via reflection, no manual DI for new sources.

Two kinds of sources in `EventScraper/Sources/`:
- **Structured** (no LLM): `JkpgSource` (embedded JSON), `NassjoSource`, `HaboSource`, `VarnamoSource` (Cruncho API), `TranasSource` (WP REST + LLM for dates in free text)
- **LLM-based**: subclass `LlmHtmlSource` — discovers URLs (`FromSitemapAsync` / `FromListPageAsync`), fetches pages, strips HTML (`HtmlTextExtractor`), sends text to `ILlmExtractor`. Skips links already in DB before any LLM call. Small ones live together in `LlmSources.cs` (Mullsjö, Sävsjö, Gislaved, Eksjö, Vetlanda, Aneby, Gnosjö, Vaggeryd)

`ScraperPipeline.RunAllAsync()` runs all `IEventSource` with `SemaphoreSlim(5)`, dedupes by `(Title.lower, StartDate)`, backfills empty `Category` via `EventCategorizer`, saves per source via `IEventRepository`. `ScraperHostedService` runs the pipeline every 6 hours; disable with `Scraper:Enabled=false` (appsettings) or env `Scraper__Enabled=false`.

### LLM extraction
`MlxExtractor` (`ILlmExtractor`) calls a local oMLX server (OpenAI-compatible, `LlmSettings` in appsettings: BaseUrl/Model/ApiKey). Plain completion + JSON clipping — `response_format: json_object` is broken in oMLX. Strips Qwen `<think>` blocks. Prompt asks for title/dates/times/location/description/imageUrl/category in one call.

### Categorization
`EventScraper/Categorization/EventCategorizer.cs` = single source of truth, 15 fixed Swedish categories:
- LLM sources: model picks category in the extraction prompt; answer validated with `EventCategorizer.Normalize()` (case/och-&/partial tolerant)
- Structured sources + invalid LLM answers: `EventCategorizer.Categorize(title, description)` — whole-word keyword scoring, title hits weigh 3x, priority list breaks ties, default `"Övrigt"`
- Tests in `Tests/Events/EventCategorizerTests.cs`

### Adding a scraper source
LLM site: subclass `LlmHtmlSource` (implement `Name`, `Municipality`, `BaseUrl`, `DiscoverUrlsAsync`). Structured site: implement `IEventSource` directly. Done — auto-discovered at startup.

### Frontend
React 19 + TypeScript + Vite. State: MobX (`mobx-react-lite`). Data fetching: TanStack Query with infinite scroll (`useInView` sentinel). UI: Tailwind CSS v4 + shadcn/ui (Radix primitives) + MUI components. Routing: React Router v7.

Infinite scroll pattern: last `EventCard` gets an intersection observer ref → triggers `fetchNextPage()` → appends next cursor page to `eventsGroup.pages`.

## Configuration

`API/appsettings.Development.json` needs `ConnectionStrings.DefaultConnection`, `ElasticSettings.Url`/`DefaultIndex`, and `LlmSettings.BaseUrl`/`Model`/`ApiKey` (local oMLX server, port 8000). Elasticsearch password hardcoded in `ElasticService.cs` (`"Password"`) — only used in dev with self-signed cert + `AllowAll` callback.

`Domain.Event.Link` has a unique index — deduplication relies on this at the DB level too.

# FindEvents

Swedish event aggregation app for Jönköping County. Scrapes municipal websites and structured feeds, presents events in a searchable, filterable feed.

## Stack

| Layer | Tech |
|---|---|
| API | .NET 10 / ASP.NET Core (minimal APIs) |
| Database | PostgreSQL 17 + pg_trgm (fuzzy search) |
| LLM extraction | Mistral API (default) or local oMLX server |
| Public frontend | Blazor Server (Interactive SSR) — served by the API |
| Alt frontend | React 19 + TypeScript + Vite (`client/`) |
| UI | Tailwind CSS v4 + shadcn/ui (React) / scoped CSS (Blazor) |

## Project structure

```
API/                  ASP.NET Core Web API + Blazor public frontend
  Components/         Blazor components
    Layout/           MainLayout (navbar)
    Pages/Public/     HomePage (/) + EventsPage (/evenemang)
  Features/           Vertical slice handlers (GetEventsHandler)
  wwwroot/            Static assets (public.css)
Application/          Shared core: Result, PagedList, cursor, DTOs
Domain/               Entity models
Persistence/          EF Core DbContext + migrations (pg_trgm enabled)
Infrastructure/       Event repository
EventScraper/         Scraper library: sources, LLM extraction, categorization
Tests/                xunit tests
client/               React frontend (alternative)
docker-compose.yaml
```

## Public frontend

The Blazor frontend is served directly by the API — no separate build step or server needed.

| Route | Description |
|---|---|
| `/` | Hero landing page |
| `/evenemang` | Filterable event grid |

Filters: text search (trigram fuzzy), kategori, kommun, arrangör, startdatum (custom calendar). Load-more cursor pagination, 16 events per page.

## Scraped sources

**Structured (no LLM)** — parsed from APIs or embedded JSON:

| Source | Municipality |
|---|---|
| [jkpg.com](https://jkpg.com) | Jönköping |
| [habokommun.se](https://www.habokommun.se) | Habo |
| [varnamo.cruncho.co](https://varnamo.cruncho.co) | Värnamo |
| [lokal.app](https://savsjo.appen.se) | Sävsjö, Vetlanda, Nässjö, Eksjö |
| [sv.se](https://www.sv.se) | Hela Jönköpings län (Studieförbundet Vuxenskolan) |

**LLM-extracted** — event pages discovered via sitemap, text sent to LLM:

| Source | Municipality |
|---|---|
| [tranas.se](https://tranas.se) | Tranås (WP REST API + LLM for dates) |
| [mullsjo.se](https://www.mullsjo.se) | Mullsjö |
| [gislaved.se](https://www.gislaved.se) | Gislaved |
| [aneby.se](https://www.aneby.se) | Aneby |
| [gnosjoandan.com](https://www.gnosjoandan.com) | Gnosjö |
| [vaggeryd.se](https://www.vaggeryd.se) | Vaggeryd |

Scraping runs every 24 hours in the background. Already-saved links are skipped before any LLM call. Each event gets one of 15 fixed Swedish categories — picked by the LLM during extraction, or by keyword scoring for structured sources, with LLM fallback for "Övrigt".

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com)
- [Node.js 20+](https://nodejs.org) — only needed for the React client

### 1. Configure secrets

```bash
cp .env.example .env
cp API/appsettings.Development.json.example API/appsettings.Development.json
```

Fill in `.env` (Postgres credentials) and `appsettings.Development.json` (connection string + LLM key).

### 2. Start the database

```bash
docker compose up postgres -d
```

### 3. Run the API + Blazor frontend

```bash
dotnet run --project API
```

Starts on `http://localhost:5001`. On first launch it runs EF Core migrations (including pg_trgm) and starts the scraper in the background. The Blazor public frontend is available at the same address.

### 4. (Optional) Run the React frontend

```bash
cd client
npm install
npm run dev
```

Runs on `http://localhost:5173` — calls the same API.

### Run tests

```bash
dotnet test Tests/Tests.csproj
```

---

## Configuration

| File | Contains |
|---|---|
| `.env` | Docker Compose variables (Postgres/pgAdmin credentials, ports) |
| `API/appsettings.Development.json` | `ConnectionStrings`, `MistralSettings`, `LlmSettings`, `Scraper:Enabled` |

**LLM extraction** — if `MistralSettings:ApiKey` is set, uses Mistral API. Otherwise falls back to a local OpenAI-compatible server (`LlmSettings:BaseUrl`, default `http://127.0.0.1:8000/v1`).

**Disable scraper** — set `Scraper:Enabled: false` (or env `Scraper__Enabled=false`) to prevent background scraping, useful during development.

**Test a single source** — comment out sources in `EventScraper/ScraperServiceExtensions.cs`.

---

## Search

Full-text search uses PostgreSQL `pg_trgm` — no separate search service needed. Supports case-insensitive substring match (`ILIKE`) and fuzzy trigram similarity on title, description, location and municipality.

---

## Adding a scraper source

All sources are registered in `EventScraper/ScraperServiceExtensions.cs`. Add a line and implement the interface.

**Structured source** (API or JSON): implement `IEventSource` in `EventScraper/Sources/`.

**LLM-extracted source**: subclass `LlmHtmlSource` and implement `Name`, `Municipality`, `BaseUrl` and `DiscoverUrlsAsync()`. Use `FromSitemapAsync()` or `FromListPageAsync()` for URL discovery. The base class handles HTML fetching, text extraction, LLM extraction and deduplication.

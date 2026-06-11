# FindEvents

Swedish event aggregation app for Jönköping County. Scrapes event listings from municipal websites — structured feeds where available, local LLM extraction where not — and presents them in a searchable, filterable feed.

## Stack

| Layer | Tech |
|---|---|
| API | .NET 10 / ASP.NET Core (minimal APIs) |
| Database | PostgreSQL 17 |
| Search | Elasticsearch 8 (optional) |
| LLM extraction | Local oMLX server (OpenAI-compatible, optional) |
| Frontend | React + TypeScript + Vite |
| State | MobX |
| UI | Tailwind CSS + shadcn/ui |

## Project structure

```
API/              ASP.NET Core Web API (vertical slice features + modules)
Application/      Shared core: Result, PagedList, cursor, DTOs
Domain/           Entity models
Persistence/      EF Core DbContext + migrations
Infrastructure/   Event repository
EventScraper/     Scraper library: sources, LLM extraction, categorization
EventTrainer/     Dev utility: keyword backfill of categories in DB
Tests/            xunit tests
client/           React frontend
docker-compose.yaml
```

## Scraped sources

**Structured (no LLM)** — parsed from embedded JSON or APIs:

- [jkpg.com](https://jkpg.com) — Jönköping
- [nassjo.se](https://www.nassjo.se) — Nässjö
- [habokommun.se](https://www.habokommun.se) — Habo
- [varnamo.cruncho.co](https://varnamo.cruncho.co) — Värnamo
- [tranas.se](https://tranas.se) — Tranås (WP REST + LLM for dates)

**LLM-extracted** — event pages discovered via sitemap/list page, text sent to a local LLM:

- Mullsjö, Sävsjö, Gislaved, [Eksjö](https://visiteksjo.se), Vetlanda, Aneby, Gnosjö, Vaggeryd

Scraping runs automatically every 6 hours in the background. Already-saved links are skipped, so LLM calls only happen for new events. Each event gets one of 15 fixed categories — picked by the LLM during extraction, or by keyword matching for structured sources.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [Docker](https://www.docker.com)
- (Optional) Local oMLX server for LLM-based sources

### 1. Start the database

```bash
docker compose up postgres -d
```

> To also start Elasticsearch + Kibana (optional, enables full-text search):
> ```bash
> docker compose up -d
> ```

### 2. Run the API

```bash
dotnet run --project API
```

The API starts on `http://localhost:5001`. On first launch it:
- Runs EF Core migrations automatically
- Starts the event scraper in the background

> Events appear in the feed after the first scraper run. Without an LLM server, only structured sources produce events.

### 3. Run the frontend

```bash
cd client
npm install
npm run dev
```

Frontend runs on `http://localhost:5173`.

### Run tests

```bash
dotnet test Tests/Tests.csproj
```

---

## Configuration

### `API/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=findevents;Username=postgres;Password=changeme123"
  },
  "ElasticSettings": {
    "Url": "https://localhost:9200",
    "DefaultIndex": "events"
  },
  "LlmSettings": {
    "BaseUrl": "http://127.0.0.1:8000/v1",
    "Model": "Qwen3.5-4B-MLX-4bit",
    "ApiKey": "apikey"
  },
  "Scraper": {
    "Enabled": true
  }
}
```

- Elasticsearch is optional — if unreachable, search falls back to SQL `LIKE` queries.
- `LlmSettings` points to a local OpenAI-compatible server (oMLX). Without it, LLM-based sources are skipped.
- `Scraper:Enabled` (or env `Scraper__Enabled=false`) disables the background scraper, useful during development.

### `.env` (Docker Compose variables)

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=changeme123
POSTGRES_DB=findevents
POSTGRES_PORT=5432

STACK_VERSION=8.15.0
ELASTIC_PASSWORD=changeme123
KIBANA_PASSWORD=kibana456
ES_PORT=9200
KIBANA_PORT=5601
MEM_LIMIT=1073741824
```

---

## Adding a scraper source

All sources implement `IEventSource` and are auto-discovered at startup — no manual DI registration.

**Site with structured data** (JSON blob, API): implement `IEventSource` directly in `EventScraper/Sources/`.

**Site without structured data**: subclass `LlmHtmlSource` and implement `Name`, `Municipality`, `BaseUrl` and `DiscoverUrlsAsync()` (use `FromSitemapAsync()` or `FromListPageAsync()`). The base class fetches each page, strips the HTML and lets the LLM extract the event.

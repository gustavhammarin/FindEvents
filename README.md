# FindEvents

Swedish event aggregation app for Jönköping County. Scrapes municipal websites and structured feeds, presents events in a searchable, filterable feed.

## Stack

| Layer | Tech |
|---|---|
| Server | .NET 10 / ASP.NET Core |
| Frontend | HTMX + Razor Pages (SSR, no build step) |
| Database | PostgreSQL 17 + pgvector + pg_trgm |
| LLM extraction | Mistral API (default) or local oMLX server |
| Semantic search | Mistral embeddings → pgvector cosine similarity |

## Project structure

```
App/                    ASP.NET Core app
  Web/Pages/            Razor Pages frontend (HTMX)
    Shared/_Layout.cshtml   HTML shell
    Evenemang/          /evenemang + /evenemang/{id} pages
  Scraper/              Background scraper — sources, LLM extraction, categorization
  Persistence/          EF Core DbContext + migrations
  Services/             EventService, DTOs, cursor pagination
  Embedder/             Mistral embedding + category classifier
  wwwroot/              Static assets (css/app.css, js/htmx-filters.js)
Tests/                  xunit tests
docker-compose.yaml     Dev infra (postgres + pgadmin)
docker-compose.prod.yaml  Production (app + postgres + Caddy)
```

## Frontend

Server-rendered with Razor Pages. HTMX handles partial updates — no page reload on filter changes or load more.

| Route | Description |
|---|---|
| `/` | Redirects to `/evenemang` |
| `/evenemang` | Filterable event grid |
| `/evenemang/{id}` | Event detail + similar events |

Filters: text search (trigram fuzzy + semantic), kategori, plats, startdatum (custom calendar). Cursor-based pagination, 32 events per page.

## Scraped sources

**Structured (no LLM):**

| Source | Municipality |
|---|---|
| [jkpg.com](https://jkpg.com) | Jönköping |
| [habokommun.se](https://www.habokommun.se) | Habo |
| [varnamo.cruncho.co](https://varnamo.cruncho.co) | Värnamo |
| [sv.se](https://www.sv.se) | Hela Jönköpings län (Studieförbundet Vuxenskolan) |

**LLM-extracted** — event pages discovered via sitemap, text sent to LLM:

| Source | Municipality |
|---|---|
| [tranas.se](https://tranas.se) | Tranås |
| [mullsjo.se](https://www.mullsjo.se) | Mullsjö |
| [savsjo.appen.se](https://savsjo.appen.se) | Sävsjö |
| [gislaved.se](https://www.gislaved.se) | Gislaved |
| [aneby.se](https://www.aneby.se) | Aneby |
| [gnosjoandan.com](https://www.gnosjoandan.com) | Gnosjö |
| [vaggeryd.se](https://www.vaggeryd.se) | Vaggeryd |

Scraping runs every 24 hours. Already-saved links are skipped before any LLM call. Each event gets one of 15 fixed Swedish categories — picked by the LLM during extraction, or by keyword scoring for structured sources.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com)

### 1. Configure secrets

```bash
cp .env.example .env
```

One file for everything — Postgres credentials, connection string, `MistralSettings__ApiKey`, admin password. Loaded automatically by both the app (DotNetEnv) and docker compose.

### 2. Start the database

```bash
docker compose up postgres -d
```

### 3. Run

```bash
dotnet run --project App
```

Starts on `http://localhost:5001`. Runs EF Core migrations on first launch and starts the scraper in the background.

### Run tests

```bash
dotnet test Tests/Tests.csproj
```

---

## Configuration

All secrets and environment-specific config live in a single gitignored `.env` at the repo root (template: `.env.example`). App settings use double-underscore keys: `MistralSettings__ApiKey` → `MistralSettings:ApiKey`. Non-secret defaults live in `App/appsettings.json`.

**LLM extraction** — `MistralSettings__ApiKey` uses Mistral API. Falls back to local OpenAI-compatible server (`LlmSettings:BaseUrl`, default `http://127.0.0.1:8000/v1`) when the key is empty.

**Disable scraper** — set `Scraper__Enabled=false`.

**Admin page** — `/admin` shows scrape/embedding run history and database stats. Protected by HTTP Basic Auth (user `admin`, password `Admin__Password`). Disabled (404) when no password is set.

---

## Deployment

Docker Compose on any VPS: app + Postgres + [Caddy](https://caddyserver.com) (automatic HTTPS via Let's Encrypt).

```bash
# On the server
git clone git@github.com:gustavhammarin/FindEvents.git && cd FindEvents
cp .env.example .env    # fill in real values; set DOMAIN and Host=postgres in the connection string
docker compose -f docker-compose.prod.yaml up -d --build
```

Point the domain's DNS A record at the server before starting — Caddy needs it to issue the TLS certificate. Migrations run automatically at app startup.

Update to a new version:

```bash
git pull && docker compose -f docker-compose.prod.yaml up -d --build
```

**Health** — `/healthz` returns 200 when the app and database are up (used by the compose healthcheck; also handy for any external uptime monitor).

---

## Search

Full-text search: PostgreSQL `pg_trgm` — case-insensitive substring + fuzzy trigram on title, description, location, municipality.

Semantic search: query embedded via Mistral → cosine similarity on pgvector column → results re-ranked with text matches.

---

## Adding a scraper source

All sources implement `IEventSource` and are registered in `App/Scraper/ScraperServiceExtensions.cs`.

**Structured source**: implement `IEventSource` in `App/Scraper/Sources/`.

**LLM-extracted source**: subclass `LlmHtmlSource`, implement `Name`, `Municipality`, `BaseUrl`, `DiscoverUrlsAsync()`. Use `FromSitemapAsync()` or `FromListPageAsync()` for URL discovery.

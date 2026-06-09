# FindEvents

Swedish event aggregation app for Småland. Scrapes event listings from municipal websites and presents them in a searchable, filterable feed.

## Stack

| Layer | Tech |
|---|---|
| API | .NET 10 / ASP.NET Core |
| Database | PostgreSQL 17 |
| Search | Elasticsearch 8 (optional) |
| Frontend | React + TypeScript + Vite |
| State | MobX |
| UI | Tailwind CSS + shadcn/ui |

## Project structure

```
API/              ASP.NET Core Web API
Application/      MediatR handlers, queries, interfaces
Domain/           Entity models
Persistence/      EF Core DbContext + migrations
Infrastructure/   Elasticsearch, event repository
EventScraper/     Scraper library (runs as background service in API)
EventTrainer/     ML tool for category classification (dev utility)
client/           React frontend
docker-compose.yaml
```

## Scraped sources

- [jkpg.com](https://jkpg.com) — Jönköping
- [visiteksjo.se](https://visiteksjo.se) — Eksjö
- [gislaved.se](https://www.gislaved.se) — Gislaved
- [tranas.se](https://www.tranas.se) — Tranås
- [varnamo.se](https://www.varnamo.se) — Värnamo

Scraping runs automatically every 6 hours in the background. New events appear in the feed as soon as a run completes.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- [Docker](https://www.docker.com)

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

> Events appear in the feed after the first scraper run (~2–5 min).

### 3. Run the frontend

```bash
cd client
npm install
npm run dev
```

Frontend runs on `http://localhost:5173`.

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
  }
}
```

Elasticsearch is optional — if unreachable, search falls back to SQL `LIKE` queries.

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

## Adding a scraper

1. Create a class in `EventScraper/Scrapers/` that extends `BaseScraper`
2. Implement `GetPageUrlsAsync()` and `ParseEvent()`
3. It is automatically discovered and registered at startup — no manual DI registration needed

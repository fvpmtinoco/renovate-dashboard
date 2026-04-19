# RenovateDashboard — Scaffold Design

**Date:** 2026-04-18
**Scope:** Initial solution structure, Dockerfiles, and docker-compose scaffold

---

## Stack

- **Backend:** .NET 10 Minimal API + BackgroundService (`RenovateDashboard.App`)
- **Frontend:** React + Vite, served as static files via nginx (`RenovateDashboard.Web`)
- **Email:** Resend .NET SDK
- **GitLab:** GitLab API called server-side (token never exposed to browser)

---

## Directory Structure

```
renovate-dashboard/
├── RenovateDashboard.sln
├── docker-compose.yml
├── .env.example
├── docs/
│   └── superpowers/specs/
└── src/
    ├── RenovateDashboard.App/
    │   ├── RenovateDashboard.App.csproj
    │   ├── Dockerfile
    │   ├── Program.cs                  # DI wiring + app.MapMrEndpoints()
    │   ├── Endpoints/
    │   │   └── MrEndpoints.cs          # IEndpointRouteBuilder extension
    │   ├── Services/
    │   │   ├── GitLabService.cs        # Fetches open Renovate MRs from GitLab API
    │   │   └── DigestService.cs        # Builds + sends email digest via Resend SDK
    │   ├── Workers/
    │   │   └── DigestWorker.cs         # BackgroundService, cron-driven
    │   └── appsettings.json
    └── RenovateDashboard.Web/
        ├── Dockerfile
        ├── package.json                # Vite + React
        ├── vite.config.ts
        ├── index.html
        └── src/
            ├── main.tsx
            └── App.tsx
```

---

## API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `GET` | `/api/mrs` | Returns all open Renovate MRs across configured repos (live from GitLab) |
| `POST` | `/api/digest/send` | Manually triggers the email digest |
| `GET` | `/health` | Health check for Docker |

All route handlers live in `Endpoints/MrEndpoints.cs` as an `IEndpointRouteBuilder` extension method. `Program.cs` calls `app.MapMrEndpoints()` and handles only builder/middleware setup.

---

## Data Flow

```
React (browser) → GET /api/mrs → GitLabService → GitLab API
                                                       ↑
DigestWorker (cron) ──────────────────────────────────┘
     └──→ DigestService → Resend API → email recipients
```

- No local caching. Every request to `/api/mrs` hits the GitLab API live.
- `DigestWorker` runs on a configurable cron (default `0 9 * * 1`).
- Renovate MRs are identified by the `renovate` label on GitLab MRs.

---

## Docker Compose

```yaml
services:
  api:
    build: ./src/RenovateDashboard.App
    ports: ["5000:8080"]
    env_file: .env

  web:
    build: ./src/RenovateDashboard.Web
    ports: ["3000:80"]
    depends_on: [api]
```

Two separate containers. No shared volumes or custom networks needed at this stage.

---

## Dockerfiles

**API** (`src/RenovateDashboard.App/Dockerfile`) — multi-stage:
1. `mcr.microsoft.com/dotnet/sdk:10.0` — restore + publish
2. `mcr.microsoft.com/dotnet/aspnet:10.0` — runtime, copy publish output

**Web** (`src/RenovateDashboard.Web/Dockerfile`) — two-stage:
1. `node:22-alpine` — `npm ci` + `npm run build` → `dist/`
2. `nginx:alpine` — serve `dist/` on port 80

---

## Environment Variables (`.env.example`)

```
GITLAB_TOKEN=
GITLAB_URL=https://gitlab.com
GITLAB_REPOS=
RESEND_API_KEY=
DIGEST_RECIPIENTS=
DIGEST_CRON=0 9 * * 1
```

All six variables are consumed by the API container only. The React container has no runtime env vars.

---

## Out of Scope

- User authentication
- Storing MR history
- Per-repo configuration via UI
- Shared contracts/core project

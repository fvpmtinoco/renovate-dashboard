# Renovate Dashboard

Internal tool to track open Renovate Bot MRs across GitLab repos and send periodic email digests.

## Stack

- **Backend:** .NET 10 Minimal API + BackgroundService
- **Frontend:** React + Vite, served by nginx
- **Email:** [Resend](https://resend.com)
- **GitLab:** Server-side API calls (token never exposed to browser)

## Getting Started

1. Copy the env template and fill in your values:

   ```bash
   cp .env.example .env
   ```

2. Start both services:

   ```bash
   docker compose up --build
   ```

3. Open [http://localhost:3000](http://localhost:3000) for the dashboard.  
   The API is available at [http://localhost:5000](http://localhost:5000).

## Environment Variables

| Variable | Description |
|----------|-------------|
| `GITLAB_TOKEN` | Personal access token with `read_api` scope |
| `GITLAB_URL` | GitLab instance URL (e.g. `https://gitlab.com`) |
| `GITLAB_REPOS` | Comma-separated `namespace/repo` slugs |
| `RESEND_API_KEY` | Resend API key |
| `DIGEST_RECIPIENTS` | Comma-separated recipient email addresses |
| `DIGEST_CRON` | Cron expression for digest schedule (default: `0 9 * * 1`, Monday 9am UTC) |

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/health` | Health check |
| `GET` | `/api/mrs` | List open Renovate MRs across all configured repos |
| `POST` | `/api/digest/send` | Manually trigger the email digest |

## Deployment

Copy `docker-compose.yml` and `.env` to any Linux host with Docker, then:

```bash
docker compose up -d
```

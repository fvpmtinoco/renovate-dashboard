# RenovateDashboard

Internal tool to track open Renovate Bot MRs across GitLab repos and send periodic email digests.

## Architecture

- `src/RenovateDashboard.App` — .NET Minimal API + BackgroundService (single process)
- `src/RenovateDashboard.Web` — Frontend (Blazor or React TBD)
- `docker-compose.yml` — orchestrates both services locally and on VPS

## Key decisions

- Email via Resend (not self-hosted). Use the Resend .NET SDK.
- Config via `.env` / environment variables (no database for now)
- GitLab API is the source of truth — no local caching of MR state
- BackgroundService runs a configurable cron (default: Monday 9am)
- Renovate MRs are identified by the `renovate` label on GitLab MRs

## Configuration (env vars)

- `GITLAB_TOKEN` — personal access token with read_api scope
- `GITLAB_URL` — e.g. https://gitlab.com
- `GITLAB_REPOS` — comma-separated list of namespace/repo slugs
- `RESEND_API_KEY`
- `DIGEST_RECIPIENTS` — comma-separated emails
- `DIGEST_CRON` — cron expression, default `0 9 * * 1`

## Dev setup

docker compose up --build

## Deployment

Target: Hetzner VPS (or any Linux host with Docker). Copy docker-compose.yml + .env and run docker compose up -d.

## Out of scope (for now)

- User authentication
- Storing MR history
- Per-repo configuration via UI
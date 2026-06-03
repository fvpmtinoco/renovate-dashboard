# Auth0 Authentication — Design

**Date:** 2026-06-03
**Status:** Approved (pending spec review)

## Goal

Protect RenovateDashboard with Auth0 authentication. Today both the React SPA and the
.NET API are fully open: anyone who reaches the API can read MRs (`/api/mrs`) or trigger
email digests (`/api/digest/send`). We want a real security boundary on the **API**, with
a login wall on the UI for UX.

## Context

- **Frontend**: React 19 SPA (Vite), served as static files. Calls `/api/*`, proxied
  same-origin to the backend in every environment:
  - Dev: Vite proxy → `http://localhost:7146`
  - Docker: nginx → `http://api:8080`
  - Prod: Vercel rewrite (`src/RenovateDashboard.Web/vercel.json`) → `https://renovate-dashboard.onrender.com`
- **Backend**: .NET Minimal API on Render. Endpoints: `/api/mrs`, `/api/digest/send`, `/health`.
- An Auth0 **SPA application** and an Auth0 **API (resource server / audience)** already exist.
- An Auth0 **Action** already restricts logins to `@neuraspace.com` Google accounts.

## Key decisions

- **Security boundary = the API.** The SPA login wall is for UX; the JWT validation on the
  .NET API is what actually protects the data.
- **Authorization is upstream.** The `@neuraspace.com` restriction lives in the Auth0 Action.
  The app therefore only requires a *valid authenticated token* — no roles, permissions, or
  allowlists in our code.
- **No CORS needed.** All three environments proxy `/api` same-origin and forward the
  `Authorization` header transparently.
- Reuse the existing Auth0 API audience; no new resource server.

## Auth flow

1. User opens the SPA → unauthenticated → redirected to Auth0 Universal Login → Google →
   Auth0 Action enforces `@neuraspace.com` → redirect back, SDK exchanges code for tokens.
2. SPA silently obtains an **access token for the existing API audience**.
3. SPA calls `/api/mrs` with `Authorization: Bearer <token>`; the same-origin proxy forwards
   it to the .NET API.
4. .NET validates issuer, audience, and signature (Auth0 JWKS) → returns data, or 401.

## Frontend changes (`src/RenovateDashboard.Web`)

- Add dependency `@auth0/auth0-react`.
- `main.tsx`: wrap `<App />` in `<Auth0Provider>` configured from Vite env vars:
  - `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`
  - `authorizationParams`: `{ redirect_uri: window.location.origin, audience: VITE_AUTH0_AUDIENCE }`
  - `useRefreshTokens: true`, `cacheLocation: 'memory'` (default) for reliable silent renewal.
- Gate the UI with `withAuthenticationRequired(App)` — no public view; unauthenticated users
  auto-redirect to login. Render a "Redirecting to login…" fallback.
- `App.tsx`: obtain the token via `getAccessTokenSilently()` and attach
  `Authorization: Bearer <token>` to the `/api/mrs` fetch. On token retrieval / 401 failure,
  trigger `loginWithRedirect()`.
- Header: add a **Logout** button and show the signed-in user's email.
- `.env.example` (new, for the Web project) documenting the three `VITE_AUTH0_*` vars.

## Backend changes (`src/RenovateDashboard.App`)

- Add package `Microsoft.AspNetCore.Authentication.JwtBearer`.
- `Program.cs`: configure JWT Bearer authentication:
  - `Authority = https://{AUTH0_DOMAIN}/`
  - `Audience = {AUTH0_AUDIENCE}`
  - Default token validation (issuer, audience, signature via JWKS).
- Add `app.UseAuthentication();` and `app.UseAuthorization();`.
- `MrEndpoints`: `.RequireAuthorization()` on `/api/mrs` and `/api/digest/send`.
  `/health` stays **anonymous** (Docker/Render healthchecks need it).
- The cron `DigestWorker` calls `DigestService` in-process, so protecting the HTTP
  endpoint does not affect scheduled digests.
- Config via env vars `AUTH0__DOMAIN`, `AUTH0__AUDIENCE`; add to `.env.example`.

## Auth0 dashboard setup (SPA application)

Add to the SPA app's allowed URLs (Callback, Logout, Web Origins):
- `http://localhost:5173` (Vite dev)
- `http://localhost:3000` (Docker web)
- `https://renovate-dashboard.vercel.app` (prod)

The API/audience already exists — reuse its identifier as `VITE_AUTH0_AUDIENCE` and `AUTH0__AUDIENCE`.

## Deployment / config wiring

- **Vercel (frontend)**: set `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`
  as Vercel build-time environment variables.
- **Docker (frontend)**: the web image is a static build, so `VITE_AUTH0_*` are baked at
  *build* time. Pass them as **build args** in the web `Dockerfile` and `docker-compose.yml`.
- **Render (backend)**: set `AUTH0__DOMAIN`, `AUTH0__AUDIENCE` as runtime env vars.

## Testing

- **Backend (in scope):** one integration test via `WebApplicationFactory` asserting
  `/api/mrs` returns **401 without a token**. Existing `GitLabServiceTests` unchanged.
- **Frontend:** intentionally skipped (per decision) — rely on the backend test.

## Out of scope

- Roles / permissions / RBAC (handled by the Auth0 Action).
- CORS configuration (not needed; same-origin proxying).
- Refresh-token rotation policy tuning beyond enabling `useRefreshTokens`.

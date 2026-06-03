# Auth0 Authentication Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Protect the RenovateDashboard API with Auth0 JWT validation and put a login wall on the React SPA, so only authenticated users (restricted to `@neuraspace.com` by an existing Auth0 Action) can read MRs or trigger digests.

**Architecture:** The .NET Minimal API validates Auth0-issued JWT access tokens (issuer + audience + signature via JWKS) and requires authorization on `/api/mrs` and `/api/digest/send` (`/health` stays anonymous). The React SPA uses `@auth0/auth0-react` to log in, obtain an access token for the existing API audience, and send it as a `Bearer` header. All environments proxy `/api` same-origin (Vite dev, nginx, Vercel→Render), so no CORS is needed.

**Tech Stack:** .NET 10 Minimal API, `Microsoft.AspNetCore.Authentication.JwtBearer`, xUnit + `Microsoft.AspNetCore.Mvc.Testing`, React 19, Vite, `@auth0/auth0-react`.

---

## File Structure

**Backend (`src/RenovateDashboard.App`)**
- Modify `Program.cs` — register JwtBearer auth, add auth middleware, expose `Program` for tests.
- Modify `Endpoints/MrEndpoints.cs` — `.RequireAuthorization()` on protected endpoints.
- Modify `RenovateDashboard.App.csproj` — add JwtBearer package.

**Backend tests (`src/RenovateDashboard.App.Tests`)**
- Create `Endpoints/MrEndpointsAuthTests.cs` — integration test: 401 without token.
- Modify `RenovateDashboard.App.Tests.csproj` — add Mvc.Testing package.

**Frontend (`src/RenovateDashboard.Web`)**
- Modify `package.json` / `package-lock.json` — add `@auth0/auth0-react`.
- Modify `src/main.tsx` — wrap app in `<Auth0Provider>`.
- Modify `src/App.tsx` — attach Bearer token to fetch, add logout, gate with `withAuthenticationRequired`.
- Create `src/vite-env.d.ts` — type the `VITE_AUTH0_*` env vars.
- Create `.env.example` — document the three frontend vars.

**Config / deployment**
- Modify `.env.example` (root) — add backend `AUTH0__*` and compose-interpolated `VITE_AUTH0_*`.
- Modify `src/RenovateDashboard.Web/Dockerfile` — accept `VITE_AUTH0_*` build args.
- Modify `docker-compose.yml` — pass build args to the web service.

---

## Task 1: Backend — require auth on the API (TDD)

**Files:**
- Modify: `src/RenovateDashboard.App.Tests/RenovateDashboard.App.Tests.csproj`
- Modify: `src/RenovateDashboard.App/Program.cs`
- Create: `src/RenovateDashboard.App.Tests/Endpoints/MrEndpointsAuthTests.cs`
- Modify: `src/RenovateDashboard.App/RenovateDashboard.App.csproj`
- Modify: `src/RenovateDashboard.App/Endpoints/MrEndpoints.cs`

- [ ] **Step 1: Add the test-host package to the test project**

Run:
```
dotnet add src/RenovateDashboard.App.Tests package Microsoft.AspNetCore.Mvc.Testing
```
Expected: package added, version resolves to the .NET 10 line (10.0.x).

- [ ] **Step 2: Expose `Program` to the test project**

Append to the very end of `src/RenovateDashboard.App/Program.cs` (after `app.Run();`):
```csharp
public partial class Program { }
```
This makes the top-level-statement `Program` class public so `WebApplicationFactory<Program>` can reference it. (No auth yet — that comes in Step 5.)

- [ ] **Step 3: Write the failing integration test**

Create `src/RenovateDashboard.App.Tests/Endpoints/MrEndpointsAuthTests.cs`:
```csharp
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RenovateDashboard.App.Tests.Endpoints;

public class MrEndpointsAuthTests : IClassFixture<MrEndpointsAuthTests.AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public MrEndpointsAuthTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMrs_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/mrs");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public class AuthTestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Provide config so the host starts without contacting Auth0 or GitLab.
            // An unauthenticated request is rejected before either is touched.
            builder.UseSetting("Auth0:Domain", "test.eu.auth0.com");
            builder.UseSetting("Auth0:Audience", "https://test-api");
            builder.UseSetting("GitLab:Repos", "");
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it fails**

Run:
```
dotnet test src/RenovateDashboard.App.Tests --filter GetMrs_WithoutToken_Returns401
```
Expected: FAIL. With no auth configured, `/api/mrs` returns `200 OK` (empty repo list → empty array), so the assertion `Unauthorized == OK` fails.

- [ ] **Step 5: Add the JwtBearer package and configure authentication**

Run:
```
dotnet add src/RenovateDashboard.App package Microsoft.AspNetCore.Authentication.JwtBearer
```

Then edit `src/RenovateDashboard.App/Program.cs`. Add the using at the top:
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
```

After the existing `builder.Services.AddHostedService<DigestWorker>();` line, add:
```csharp
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;
    });

builder.Services.AddAuthorization();
```

After `var app = builder.Build();` and before `app.MapMrEndpoints();`, add:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

- [ ] **Step 6: Require authorization on the protected endpoints**

Edit `src/RenovateDashboard.App/Endpoints/MrEndpoints.cs`. Leave `/health` untouched. Change the two API endpoints to chain `.RequireAuthorization()`:
```csharp
app.MapGet("/api/mrs", async (GitLabService gitLab, CancellationToken ct) =>
    Results.Ok(await gitLab.GetRenovateMrsAsync(ct)))
    .RequireAuthorization();

app.MapPost("/api/digest/send", async (DigestService digest) =>
{
    await digest.SendAsync();
    return Results.NoContent();
}).RequireAuthorization();
```

- [ ] **Step 7: Run the test to verify it passes**

Run:
```
dotnet test src/RenovateDashboard.App.Tests --filter GetMrs_WithoutToken_Returns401
```
Expected: PASS. The request now returns `401 Unauthorized`.

- [ ] **Step 8: Run the full backend test suite (no regressions)**

Run:
```
dotnet test src/RenovateDashboard.App.Tests
```
Expected: all tests pass (existing `GitLabServiceTests` + `GitLabOptionsTests` + the new auth test).

- [ ] **Step 9: Commit**

```
git add src/RenovateDashboard.App src/RenovateDashboard.App.Tests
git commit -m "feat(api): require Auth0 JWT auth on MR endpoints"
```

---

## Task 2: Frontend — add Auth0Provider and login gate

**Files:**
- Modify: `src/RenovateDashboard.Web/package.json` (+ lockfile)
- Create: `src/RenovateDashboard.Web/src/vite-env.d.ts`
- Modify: `src/RenovateDashboard.Web/src/main.tsx`

- [ ] **Step 1: Install the Auth0 React SDK**

Run (from `src/RenovateDashboard.Web`):
```
npm install @auth0/auth0-react
```
Expected: `@auth0/auth0-react` added to `dependencies` in `package.json`, lockfile updated.

- [ ] **Step 2: Type the Auth0 env vars**

Create `src/RenovateDashboard.Web/src/vite-env.d.ts`:
```ts
/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_AUTH0_DOMAIN: string
  readonly VITE_AUTH0_CLIENT_ID: string
  readonly VITE_AUTH0_AUDIENCE: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
```

- [ ] **Step 3: Wrap the app in `<Auth0Provider>`**

Replace the contents of `src/RenovateDashboard.Web/src/main.tsx` with:
```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { Auth0Provider } from '@auth0/auth0-react'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Auth0Provider
      domain={import.meta.env.VITE_AUTH0_DOMAIN}
      clientId={import.meta.env.VITE_AUTH0_CLIENT_ID}
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience: import.meta.env.VITE_AUTH0_AUDIENCE,
      }}
      useRefreshTokens={true}
    >
      <App />
    </Auth0Provider>
  </StrictMode>,
)
```

- [ ] **Step 4: Commit**

```
git add src/RenovateDashboard.Web/package.json src/RenovateDashboard.Web/package-lock.json src/RenovateDashboard.Web/src/vite-env.d.ts src/RenovateDashboard.Web/src/main.tsx
git commit -m "feat(web): add Auth0Provider"
```

---

## Task 3: Frontend — gate UI, attach token, add logout

**Files:**
- Modify: `src/RenovateDashboard.Web/src/App.tsx`

- [ ] **Step 1: Update `App.tsx` to use Auth0**

Replace the contents of `src/RenovateDashboard.Web/src/App.tsx` with:
```tsx
import { useEffect, useMemo, useState } from 'react'
import { useAuth0, withAuthenticationRequired } from '@auth0/auth0-react'
import type { RenovateMrDto } from './types'
import { groupByRepo, filterGroups, getRepoUrl } from './utils/mr'
import { SearchBar } from './components/SearchBar'
import { RepoGroup } from './components/RepoGroup'

type FetchStatus = 'loading' | 'error' | 'done'

function App() {
  const { getAccessTokenSilently, logout, user } = useAuth0()
  const [mrs, setMrs] = useState<RenovateMrDto[]>([])
  const [status, setStatus] = useState<FetchStatus>('loading')
  const [query, setQuery] = useState('')

  useEffect(() => {
    const controller = new AbortController()
    ;(async () => {
      try {
        const token = await getAccessTokenSilently()
        const r = await fetch('/api/mrs', {
          signal: controller.signal,
          headers: { Authorization: `Bearer ${token}` },
        })
        if (!r.ok) throw new Error(`HTTP ${r.status}`)
        const data = (await r.json()) as RenovateMrDto[]
        setMrs(data)
        setStatus('done')
      } catch (err) {
        if ((err as Error).name !== 'AbortError') {
          console.error('[renovate-dashboard] failed to fetch /api/mrs', err)
          setStatus('error')
        }
      }
    })()
    return () => controller.abort()
  }, [getAccessTokenSilently])

  const groups = useMemo(() => groupByRepo(mrs), [mrs])
  const filtered = useMemo(() => filterGroups(groups, query), [groups, query])

  if (status === 'loading') {
    return (
      <div className="flex items-center justify-center min-h-screen text-gray-500">
        Loading…
      </div>
    )
  }

  if (status === 'error') {
    return (
      <div className="flex items-center justify-center min-h-screen text-red-600">
        Failed to load MRs. Is the backend running?
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <header className="mb-6 flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Renovate Dashboard</h1>
          <p className="text-sm text-gray-500 mt-1">
            {mrs.length} open MR{mrs.length !== 1 ? 's' : ''} across {groups.size} repo{groups.size !== 1 ? 's' : ''}
          </p>
        </div>
        <div className="flex items-center gap-3 shrink-0">
          {user?.email && <span className="text-sm text-gray-500">{user.email}</span>}
          <button
            onClick={() => logout({ logoutParams: { returnTo: window.location.origin } })}
            className="text-sm text-gray-600 hover:text-gray-900 underline"
          >
            Log out
          </button>
        </div>
      </header>

      <div className="mb-4">
        <SearchBar value={query} onChange={setQuery} />
      </div>

      {filtered.size === 0 ? (
        <p className="text-gray-500 text-sm py-8 text-center">
          {mrs.length === 0
            ? 'No open Renovate MRs found.'
            : 'No MRs match your search.'}
        </p>
      ) : (
        <div className="flex flex-col gap-3">
          {[...filtered.entries()].map(([repo, repoMrs]) => (
            <RepoGroup
              key={repo}
              repo={repo}
              mrs={repoMrs}
              repoUrl={getRepoUrl(repoMrs[0].webUrl)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

export default withAuthenticationRequired(App, {
  onRedirecting: () => (
    <div className="flex items-center justify-center min-h-screen text-gray-500">
      Redirecting to login…
    </div>
  ),
})
```

- [ ] **Step 2: Verify it type-checks and builds**

Run (from `src/RenovateDashboard.Web`):
```
npm run build
```
Expected: `tsc -b` passes (no type errors) and `vite build` produces `dist/` with no errors.

- [ ] **Step 3: Verify lint passes**

Run (from `src/RenovateDashboard.Web`):
```
npm run lint
```
Expected: no lint errors.

- [ ] **Step 4: Run existing unit tests (no regressions)**

Run (from `src/RenovateDashboard.Web`):
```
npm test
```
Expected: existing `mr.test.ts` and `relativeTime.test.ts` still pass.

- [ ] **Step 5: Commit**

```
git add src/RenovateDashboard.Web/src/App.tsx
git commit -m "feat(web): gate UI behind login and send bearer token"
```

---

## Task 4: Config and deployment wiring

**Files:**
- Create: `src/RenovateDashboard.Web/.env.example`
- Modify: `.env.example` (root)
- Modify: `src/RenovateDashboard.Web/Dockerfile`
- Modify: `docker-compose.yml`

- [ ] **Step 1: Document the frontend env vars**

Create `src/RenovateDashboard.Web/.env.example`:
```
# Auth0 SPA application settings (public values, baked into the build).
# For local dev copy to .env.local and fill in.
VITE_AUTH0_DOMAIN=your-tenant.eu.auth0.com
VITE_AUTH0_CLIENT_ID=your-spa-client-id
VITE_AUTH0_AUDIENCE=https://your-api-identifier
```
(`.env.local` is already gitignored via the `*.local` rule in `src/RenovateDashboard.Web/.gitignore`.)

- [ ] **Step 2: Add backend + compose vars to the root env example**

Append to `.env.example` (root):
```

# Auth0 — backend token validation (consumed by the api container)
AUTH0__DOMAIN=your-tenant.eu.auth0.com
AUTH0__AUDIENCE=https://your-api-identifier

# Auth0 — frontend build args (consumed by docker compose to build the web image)
VITE_AUTH0_DOMAIN=your-tenant.eu.auth0.com
VITE_AUTH0_CLIENT_ID=your-spa-client-id
VITE_AUTH0_AUDIENCE=https://your-api-identifier
```

- [ ] **Step 3: Accept build args in the web Dockerfile**

Edit `src/RenovateDashboard.Web/Dockerfile`. In the `build` stage, after `COPY . .` and before `ENV NODE_ENV=production`, insert:
```dockerfile
ARG VITE_AUTH0_DOMAIN
ARG VITE_AUTH0_CLIENT_ID
ARG VITE_AUTH0_AUDIENCE
ENV VITE_AUTH0_DOMAIN=$VITE_AUTH0_DOMAIN
ENV VITE_AUTH0_CLIENT_ID=$VITE_AUTH0_CLIENT_ID
ENV VITE_AUTH0_AUDIENCE=$VITE_AUTH0_AUDIENCE
```

- [ ] **Step 4: Pass build args from compose**

Edit `docker-compose.yml`. Replace the web service's `build: ./src/RenovateDashboard.Web` line with:
```yaml
    build:
      context: ./src/RenovateDashboard.Web
      args:
        VITE_AUTH0_DOMAIN: ${VITE_AUTH0_DOMAIN}
        VITE_AUTH0_CLIENT_ID: ${VITE_AUTH0_CLIENT_ID}
        VITE_AUTH0_AUDIENCE: ${VITE_AUTH0_AUDIENCE}
```
(The `api` service already loads `AUTH0__DOMAIN` / `AUTH0__AUDIENCE` via its existing `env_file: .env`.)

- [ ] **Step 5: Verify compose config resolves**

Run:
```
docker compose config
```
Expected: prints the merged config with the web `build.args` populated, no interpolation warnings (with values present in your local `.env`).

- [ ] **Step 6: Commit**

```
git add .env.example src/RenovateDashboard.Web/.env.example src/RenovateDashboard.Web/Dockerfile docker-compose.yml
git commit -m "chore: wire Auth0 config into env, docker build args"
```

---

## Task 5: Auth0 dashboard + hosting config (manual)

This task is configuration in the Auth0 dashboard and hosting providers — no code. Check each off once done.

- [ ] **Step 1: SPA application URLs**

In the Auth0 dashboard → Applications → your SPA app → Settings, add to each list (comma-separated):
- **Allowed Callback URLs:** `http://localhost:5173, http://localhost:3000, https://renovate-dashboard.vercel.app`
- **Allowed Logout URLs:** `http://localhost:5173, http://localhost:3000, https://renovate-dashboard.vercel.app`
- **Allowed Web Origins:** `http://localhost:5173, http://localhost:3000, https://renovate-dashboard.vercel.app`

Save. (Note the SPA app's **Domain**, **Client ID**, and the existing **API Identifier** for the steps below.)

- [ ] **Step 2: Vercel build env vars (frontend, prod)**

In Vercel → project → Settings → Environment Variables, add for Production (and Preview if desired):
- `VITE_AUTH0_DOMAIN` = your tenant domain
- `VITE_AUTH0_CLIENT_ID` = SPA Client ID
- `VITE_AUTH0_AUDIENCE` = the existing API identifier

Redeploy so the values are baked into the build.

- [ ] **Step 3: Render env vars (backend, prod)**

In Render → the `renovate-dashboard` service → Environment, add:
- `AUTH0__DOMAIN` = your tenant domain
- `AUTH0__AUDIENCE` = the existing API identifier

Save and let it redeploy.

- [ ] **Step 4: Local `.env` files**

- Root `.env`: set `AUTH0__DOMAIN`, `AUTH0__AUDIENCE`, and the `VITE_AUTH0_*` trio (for `docker compose`).
- `src/RenovateDashboard.Web/.env.local`: set the `VITE_AUTH0_*` trio (for `npm run dev`).

- [ ] **Step 5: End-to-end smoke test (prod)**

Open `https://renovate-dashboard.vercel.app`, confirm you are redirected to Auth0, log in with a `@neuraspace.com` Google account, and confirm the MR list loads. Then in a terminal, confirm the API rejects unauthenticated calls:
```
curl -i https://renovate-dashboard.onrender.com/api/mrs
```
Expected: `HTTP/1.1 401 Unauthorized`.

---

## Notes for the implementer

- **Why `Program` is made `public partial`:** top-level statements compile to an internal `Program`; `WebApplicationFactory<Program>` needs it visible to the test assembly.
- **Why the 401 test needs no network:** JwtBearer rejects a request with no `Authorization` header at the auth-middleware stage, before the endpoint (and thus `GitLabService`/Auth0 JWKS) is ever touched. The `UseSetting` calls only exist so the host builds cleanly.
- **No CORS anywhere:** Vite dev proxy, nginx, and the Vercel rewrite all forward `/api` same-origin including the `Authorization` header. Do not add CORS — it would be dead config.
- **`/health` stays anonymous** so Docker/Render healthchecks keep working.

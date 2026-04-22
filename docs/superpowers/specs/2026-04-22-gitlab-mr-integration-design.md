# GitLab MR Integration Design

**Date:** 2026-04-22
**Scope:** Implement `GitLabService` to fetch open Renovate MRs from the GitLab API, replacing the existing stub.

---

## Goal

`GET /api/mrs` returns all open Renovate MRs across configured repos, live from the GitLab API. Each MR includes the repo slug, creation date, CI pipeline status, and other relevant metadata.

---

## Configuration

A `GitLabOptions` class binds the `"GitLab"` section of `appsettings.json`:

```json
// appsettings.json (committed — no secrets)
{
  "GitLab": {
    "Token": "",
    "Url": "https://gitlab.com",
    "Repos": ""
  }
}
```

```json
// appsettings.Development.json (local only, not committed)
{
  "GitLab": {
    "Token": "your-pat-here",
    "Url": "https://gitlab.com",
    "Repos": "namespace/repo1,namespace/repo2"
  }
}
```

- `Repos` is a comma-separated string; `GitLabOptions.RepoList` splits it.
- Production/Docker overrides use double-underscore env vars: `GITLAB__TOKEN`, `GITLAB__URL`, `GITLAB__REPOS`.
- `.env.example` is updated to use the double-underscore convention.
- `appsettings.Development.json` is **not** currently in `.gitignore` — the implementation plan must add it to prevent accidental token commits.

---

## Response Shape

`RenovateMrDto` — one record per matching MR:

| Field | Type | Source |
|---|---|---|
| `Repo` | `string` | derived slug (`"namespace/project"`) |
| `Iid` | `int` | `iid` — MR number within the project |
| `Title` | `string` | `title` |
| `WebUrl` | `string` | `web_url` |
| `CreatedAt` | `DateTimeOffset` | `created_at` |
| `Author` | `string` | `author.name` |
| `SourceBranch` | `string` | `source_branch` |
| `TargetBranch` | `string` | `target_branch` |
| `PipelineStatus` | `string?` | `head_pipeline.status` — raw GitLab value (`success`, `failed`, `running`, `pending`, `canceled`, `skipped`), or `null` if no pipeline |

---

## Filter Criteria

An MR is included if **both** conditions are true:

1. Title starts with `chore(deps,renovate)` (case-insensitive)
2. Has the `renovate` label

GitLab pre-filters by label server-side (`labels=renovate` query param). Title prefix is applied client-side after the API response.

---

## Service Logic

`GitLabService.GetRenovateMrsAsync()`:

1. For each repo in `GitLabOptions.RepoList`, call:
   ```
   GET {Url}/api/v4/projects/{Uri.EscapeDataString(repo)}/merge_requests
       ?state=opened&labels=renovate&per_page=100
   ```
   with header `PRIVATE-TOKEN: {Token}`.
2. Filter results client-side: `title.StartsWith("chore(deps,renovate)", OrdinalIgnoreCase)`.
3. Map each match to `RenovateMrDto`. `head_pipeline` may be absent — `PipelineStatus` is nullable.
4. All repos are fetched in parallel via `Task.WhenAll`; results are flattened.

**Error handling:** if a single repo call fails (any exception — network, 401, 404), log a warning and return an empty list for that repo. Other repos still succeed. The endpoint returns HTTP 200 with whatever MRs are available.

---

## Wiring (Program.cs)

```csharp
builder.Services.Configure<GitLabOptions>(
    builder.Configuration.GetSection("GitLab"));

builder.Services.AddHttpClient<GitLabService>();
```

`GitLabService` receives `IOptions<GitLabOptions>` and `HttpClient` via constructor injection.

---

## Files Changed

| File | Change |
|---|---|
| `appsettings.json` | Add `GitLab` section with empty placeholder values |
| `appsettings.Development.json` | Add `GitLab` section with real dev values (not committed) |
| `Services/GitLabOptions.cs` | New — options class |
| `Services/RenovateMrDto.cs` | New — response record |
| `Services/GitLabService.cs` | Replace stub with real implementation |
| `Program.cs` | Add `Configure<GitLabOptions>` registration |
| `.env.example` | Update to `GITLAB__TOKEN`, `GITLAB__URL`, `GITLAB__REPOS` |
| `.gitignore` | Add `appsettings.Development.json` |

No new NuGet packages. `System.Text.Json` (already in ASP.NET Core) handles deserialization.

---

## Out of Scope

- Pagination beyond `per_page=100` per repo
- Caching MR state locally
- Retry logic on transient failures
- Per-repo configuration

# GitLab MR Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the `GitLabService` stub with a real GitLab API implementation that fetches open Renovate MRs (filtered by title prefix and label), returning typed DTOs with repo, dates, CI pipeline status, and other metadata.

**Architecture:** `GitLabService` takes a typed `HttpClient` + `IOptions<GitLabOptions>` + `ILogger<GitLabService>`. It fans out one API call per configured repo in parallel, applies a client-side title-prefix filter on top of the server-side label filter, and maps results to `RenovateMrDto` records. Per-repo failures are logged and swallowed so other repos still succeed. Config lives in the `"GitLab"` section of `appsettings.json`, overridable by env vars using the `GITLAB__` double-underscore convention.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, `System.Text.Json`, xUnit 2.9, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`

---

## File Map

| File | Change |
|---|---|
| `.gitignore` | Add `appsettings.Development.json` |
| `src/RenovateDashboard.App/appsettings.json` | Add `GitLab` section (empty placeholders) |
| `src/RenovateDashboard.App/appsettings.Development.json` | Add `GitLab` section (real dev values — not committed) |
| `src/RenovateDashboard.App/Services/GitLabOptions.cs` | New — options class with `Token`, `Url`, `Repos`, `RepoList` |
| `src/RenovateDashboard.App/Services/RenovateMrDto.cs` | New — response record (9 fields) |
| `src/RenovateDashboard.App/Services/GitLabService.cs` | Replace stub — real API calls, filtering, mapping |
| `src/RenovateDashboard.App/Endpoints/MrEndpoints.cs` | Pass `CancellationToken` to `GetRenovateMrsAsync` |
| `src/RenovateDashboard.App/Program.cs` | Add `Configure<GitLabOptions>` |
| `src/RenovateDashboard.App.Tests/RenovateDashboard.App.Tests.csproj` | New — xUnit test project |
| `src/RenovateDashboard.App.Tests/Services/FakeHttpMessageHandler.cs` | New — test helper |
| `src/RenovateDashboard.App.Tests/Services/GitLabServiceTests.cs` | New — service tests |
| `.env.example` | Update to `GITLAB__TOKEN`, `GITLAB__URL`, `GITLAB__REPOS` |

---

## Task 1: Security baseline

**Files:**
- Modify: `.gitignore`
- Modify: `src/RenovateDashboard.App/appsettings.Development.json`

- [ ] **Step 1: Add `appsettings.Development.json` to `.gitignore`**

Open `.gitignore` and add this line at the end (after the `dist/` line):

```
appsettings.Development.json
```

- [ ] **Step 2: Populate `appsettings.Development.json` with dev GitLab config**

Replace the entire content of `src/RenovateDashboard.App/appsettings.Development.json` with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "GitLab": {
    "Token": "your-gitlab-pat-here",
    "Url": "https://gitlab.com",
    "Repos": "namespace/repo1,namespace/repo2"
  }
}
```

- [ ] **Step 3: Verify `appsettings.Development.json` is now ignored by git**

```bash
git check-ignore -v src/RenovateDashboard.App/appsettings.Development.json
```

Expected output: `.gitignore:N:appsettings.Development.json    src/RenovateDashboard.App/appsettings.Development.json`

- [ ] **Step 4: Update `.env.example` to use double-underscore convention**

Replace the `GITLAB_*` lines in `.env.example` with:

```
GITLAB__TOKEN=
GITLAB__URL=https://gitlab.com
GITLAB__REPOS=namespace/repo1,namespace/repo2
```

- [ ] **Step 5: Commit**

```bash
git add .gitignore .env.example
git commit -m "chore: protect dev appsettings from commit, update env var convention"
```

---

## Task 2: Scaffold xUnit test project

**Files:**
- Create: `src/RenovateDashboard.App.Tests/RenovateDashboard.App.Tests.csproj`

- [ ] **Step 1: Create the test project**

```bash
dotnet new xunit -n RenovateDashboard.App.Tests -o src/RenovateDashboard.App.Tests
```

Expected: `The template "xUnit Test Project" was created successfully.`

- [ ] **Step 2: Add project reference to the app project**

```bash
dotnet add src/RenovateDashboard.App.Tests/RenovateDashboard.App.Tests.csproj reference src/RenovateDashboard.App/RenovateDashboard.App.csproj
```

Expected: `Reference '..\RenovateDashboard.App\RenovateDashboard.App.csproj' added to the project.`

- [ ] **Step 3: Add the test project to the solution**

```bash
dotnet sln add src/RenovateDashboard.App.Tests/RenovateDashboard.App.Tests.csproj
```

Expected: `Project 'src\RenovateDashboard.App.Tests\RenovateDashboard.App.Tests.csproj' added to the solution.`

- [ ] **Step 4: Delete the default `UnitTest1.cs` stub**

```bash
rm src/RenovateDashboard.App.Tests/UnitTest1.cs
```

- [ ] **Step 5: Verify the solution builds**

```bash
dotnet build RenovateDashboard.slnx
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add src/RenovateDashboard.App.Tests/
git commit -m "chore: scaffold xUnit test project"
```

---

## Task 3: GitLabOptions class

**Files:**
- Create: `src/RenovateDashboard.App/Services/GitLabOptions.cs`
- Create: `src/RenovateDashboard.App.Tests/Services/GitLabOptionsTests.cs`

- [ ] **Step 1: Write the failing test**

Create `src/RenovateDashboard.App.Tests/Services/GitLabOptionsTests.cs`:

```csharp
using RenovateDashboard.App.Services;

namespace RenovateDashboard.App.Tests.Services;

public class GitLabOptionsTests
{
    [Fact]
    public void RepoList_SplitsCommaSeparatedRepos()
    {
        var options = new GitLabOptions { Repos = "ns/repo1, ns/repo2 , ns/repo3" };
        Assert.Equal(["ns/repo1", "ns/repo2", "ns/repo3"], options.RepoList.ToArray());
    }

    [Fact]
    public void RepoList_ReturnsEmptyWhenReposIsEmpty()
    {
        var options = new GitLabOptions { Repos = "" };
        Assert.Empty(options.RepoList);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
dotnet test src/RenovateDashboard.App.Tests/ --filter "GitLabOptionsTests"
```

Expected: FAIL — `GitLabOptions` type not found.

- [ ] **Step 3: Create `GitLabOptions.cs`**

Create `src/RenovateDashboard.App/Services/GitLabOptions.cs`:

```csharp
namespace RenovateDashboard.App.Services;

public class GitLabOptions
{
    public string Token { get; set; } = "";
    public string Url { get; set; } = "https://gitlab.com";
    public string Repos { get; set; } = "";

    public IEnumerable<string> RepoList =>
        Repos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
```

- [ ] **Step 4: Run the tests to verify they pass**

```bash
dotnet test src/RenovateDashboard.App.Tests/ --filter "GitLabOptionsTests"
```

Expected: `Passed! - 2 test(s)`

- [ ] **Step 5: Commit**

```bash
git add src/RenovateDashboard.App/Services/GitLabOptions.cs src/RenovateDashboard.App.Tests/Services/GitLabOptionsTests.cs
git commit -m "feat: add GitLabOptions with RepoList helper"
```

---

## Task 4: RenovateMrDto record

**Files:**
- Create: `src/RenovateDashboard.App/Services/RenovateMrDto.cs`

- [ ] **Step 1: Create `RenovateMrDto.cs`**

Create `src/RenovateDashboard.App/Services/RenovateMrDto.cs`:

```csharp
namespace RenovateDashboard.App.Services;

public record RenovateMrDto(
    string Repo,
    int Iid,
    string Title,
    string WebUrl,
    DateTimeOffset CreatedAt,
    string Author,
    string SourceBranch,
    string TargetBranch,
    string? PipelineStatus
);
```

- [ ] **Step 2: Verify the project builds**

```bash
dotnet build src/RenovateDashboard.App/
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add src/RenovateDashboard.App/Services/RenovateMrDto.cs
git commit -m "feat: add RenovateMrDto response record"
```

---

## Task 5: GitLabService implementation (TDD)

**Files:**
- Create: `src/RenovateDashboard.App.Tests/Services/FakeHttpMessageHandler.cs`
- Create: `src/RenovateDashboard.App.Tests/Services/GitLabServiceTests.cs`
- Modify: `src/RenovateDashboard.App/Services/GitLabService.cs`

- [ ] **Step 1: Create `FakeHttpMessageHandler`**

Create `src/RenovateDashboard.App.Tests/Services/FakeHttpMessageHandler.cs`:

```csharp
using System.Net;

namespace RenovateDashboard.App.Tests.Services;

internal sealed class FakeHttpMessageHandler(
    Dictionary<string, (HttpStatusCode Status, string Body)> responses) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = request.RequestUri!.ToString();
        if (responses.TryGetValue(key, out var r))
        {
            return Task.FromResult(new HttpResponseMessage(r.Status)
            {
                Content = new StringContent(r.Body, System.Text.Encoding.UTF8, "application/json")
            });
        }
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found")
        });
    }
}
```

- [ ] **Step 2: Write the failing tests**

Create `src/RenovateDashboard.App.Tests/Services/GitLabServiceTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RenovateDashboard.App.Services;

namespace RenovateDashboard.App.Tests.Services;

public class GitLabServiceTests
{
    private const string BaseUrl = "https://gitlab.example.com";

    private static GitLabService CreateService(
        string repos,
        Dictionary<string, (HttpStatusCode, string)> responses)
    {
        var http = new HttpClient(new FakeHttpMessageHandler(responses));
        var options = Options.Create(new GitLabOptions
        {
            Token = "test-token",
            Url = BaseUrl,
            Repos = repos
        });
        return new GitLabService(http, options, NullLogger<GitLabService>.Instance);
    }

    private static string MrUrl(string repo) =>
        $"{BaseUrl}/api/v4/projects/{Uri.EscapeDataString(repo)}/merge_requests?state=opened&labels=renovate&per_page=100";

    [Fact]
    public async Task GetRenovateMrsAsync_IncludesOnlyRenovateTitledMrs()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { iid = 1, title = "chore(deps,renovate): update lodash", web_url = "https://gitlab.example.com/ns/repo/-/merge_requests/1", created_at = "2026-04-01T10:00:00Z", author = new { name = "renovate-bot" }, source_branch = "renovate/lodash", target_branch = "main", head_pipeline = new { status = "success" } },
            new { iid = 2, title = "feat: unrelated MR", web_url = "https://gitlab.example.com/ns/repo/-/merge_requests/2", created_at = "2026-04-01T11:00:00Z", author = new { name = "dev" }, source_branch = "feat/x", target_branch = "main", head_pipeline = (object?)null }
        });

        var service = CreateService("ns/repo", new() { [MrUrl("ns/repo")] = (HttpStatusCode.OK, body) });

        var result = (await service.GetRenovateMrsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("chore(deps,renovate): update lodash", result[0].Title);
        Assert.Equal("ns/repo", result[0].Repo);
        Assert.Equal(1, result[0].Iid);
        Assert.Equal("success", result[0].PipelineStatus);
        Assert.Equal("renovate-bot", result[0].Author);
        Assert.Equal("renovate/lodash", result[0].SourceBranch);
        Assert.Equal("main", result[0].TargetBranch);
    }

    [Fact]
    public async Task GetRenovateMrsAsync_NullPipelineStatusWhenNoPipeline()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { iid = 1, title = "chore(deps,renovate): update lodash", web_url = "u", created_at = "2026-04-01T10:00:00Z", author = new { name = "bot" }, source_branch = "s", target_branch = "main", head_pipeline = (object?)null }
        });

        var service = CreateService("ns/repo", new() { [MrUrl("ns/repo")] = (HttpStatusCode.OK, body) });

        var result = (await service.GetRenovateMrsAsync()).ToList();

        Assert.Single(result);
        Assert.Null(result[0].PipelineStatus);
    }

    [Fact]
    public async Task GetRenovateMrsAsync_FansOutAcrossMultipleRepos()
    {
        var body1 = JsonSerializer.Serialize(new[]
        {
            new { iid = 1, title = "chore(deps,renovate): update A", web_url = "u1", created_at = "2026-04-01T10:00:00Z", author = new { name = "bot" }, source_branch = "s1", target_branch = "main", head_pipeline = (object?)null }
        });
        var body2 = JsonSerializer.Serialize(new[]
        {
            new { iid = 2, title = "chore(deps,renovate): update B", web_url = "u2", created_at = "2026-04-01T10:00:00Z", author = new { name = "bot" }, source_branch = "s2", target_branch = "main", head_pipeline = (object?)null }
        });

        var service = CreateService("ns/repo1,ns/repo2", new()
        {
            [MrUrl("ns/repo1")] = (HttpStatusCode.OK, body1),
            [MrUrl("ns/repo2")] = (HttpStatusCode.OK, body2)
        });

        var result = (await service.GetRenovateMrsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Repo == "ns/repo1");
        Assert.Contains(result, r => r.Repo == "ns/repo2");
    }

    [Fact]
    public async Task GetRenovateMrsAsync_ReturnsOtherReposWhenOneRepoFails()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { iid = 1, title = "chore(deps,renovate): update A", web_url = "u", created_at = "2026-04-01T10:00:00Z", author = new { name = "bot" }, source_branch = "s", target_branch = "main", head_pipeline = (object?)null }
        });

        var service = CreateService("ns/repo1,ns/repo2", new()
        {
            [MrUrl("ns/repo1")] = (HttpStatusCode.OK, body),
            [MrUrl("ns/repo2")] = (HttpStatusCode.Unauthorized, "")
        });

        var result = (await service.GetRenovateMrsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("ns/repo1", result[0].Repo);
    }

    [Fact]
    public async Task GetRenovateMrsAsync_TitleFilterIsCaseInsensitive()
    {
        var body = JsonSerializer.Serialize(new[]
        {
            new { iid = 1, title = "CHORE(DEPS,RENOVATE): update lodash", web_url = "u", created_at = "2026-04-01T10:00:00Z", author = new { name = "bot" }, source_branch = "s", target_branch = "main", head_pipeline = (object?)null }
        });

        var service = CreateService("ns/repo", new() { [MrUrl("ns/repo")] = (HttpStatusCode.OK, body) });

        var result = (await service.GetRenovateMrsAsync()).ToList();

        Assert.Single(result);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test src/RenovateDashboard.App.Tests/ --filter "GitLabServiceTests"
```

Expected: FAIL — `GitLabService` constructor signature mismatch.

- [ ] **Step 4: Replace `GitLabService.cs` with full implementation**

Overwrite `src/RenovateDashboard.App/Services/GitLabService.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace RenovateDashboard.App.Services;

public class GitLabService
{
    private readonly HttpClient _http;
    private readonly GitLabOptions _options;
    private readonly ILogger<GitLabService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GitLabService(HttpClient http, IOptions<GitLabOptions> options, ILogger<GitLabService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<RenovateMrDto>> GetRenovateMrsAsync(CancellationToken ct = default)
    {
        var results = await Task.WhenAll(_options.RepoList.Select(repo => FetchMrsAsync(repo, ct)));
        return results.SelectMany(x => x);
    }

    private async Task<IEnumerable<RenovateMrDto>> FetchMrsAsync(string repo, CancellationToken ct)
    {
        try
        {
            var encoded = Uri.EscapeDataString(repo);
            var url = $"{_options.Url}/api/v4/projects/{encoded}/merge_requests?state=opened&labels=renovate&per_page=100";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("PRIVATE-TOKEN", _options.Token);

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var mrs = await JsonSerializer.DeserializeAsync<List<GitLabMrResponse>>(
                response.Content.ReadAsStream(), JsonOpts, ct) ?? [];

            return mrs
                .Where(mr => mr.Title.StartsWith("chore(deps,renovate)", StringComparison.OrdinalIgnoreCase))
                .Select(mr => new RenovateMrDto(
                    Repo: repo,
                    Iid: mr.Iid,
                    Title: mr.Title,
                    WebUrl: mr.WebUrl,
                    CreatedAt: mr.CreatedAt,
                    Author: mr.Author?.Name ?? "",
                    SourceBranch: mr.SourceBranch,
                    TargetBranch: mr.TargetBranch,
                    PipelineStatus: mr.HeadPipeline?.Status
                ));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch MRs for repo {Repo}", repo);
            return [];
        }
    }

    private sealed class GitLabMrResponse
    {
        [JsonPropertyName("iid")]
        public int Iid { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("web_url")]
        public string WebUrl { get; set; } = "";

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("author")]
        public GitLabAuthor? Author { get; set; }

        [JsonPropertyName("source_branch")]
        public string SourceBranch { get; set; } = "";

        [JsonPropertyName("target_branch")]
        public string TargetBranch { get; set; } = "";

        [JsonPropertyName("head_pipeline")]
        public GitLabPipeline? HeadPipeline { get; set; }
    }

    private sealed class GitLabAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    private sealed class GitLabPipeline
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "";
    }
}
```

- [ ] **Step 5: Run tests to verify they all pass**

```bash
dotnet test src/RenovateDashboard.App.Tests/ --filter "GitLabServiceTests"
```

Expected: `Passed! - 5 test(s)`

- [ ] **Step 6: Run all tests**

```bash
dotnet test src/RenovateDashboard.App.Tests/
```

Expected: all tests pass, 0 failures.

- [ ] **Step 7: Commit**

```bash
git add src/RenovateDashboard.App/Services/GitLabService.cs \
        src/RenovateDashboard.App.Tests/Services/FakeHttpMessageHandler.cs \
        src/RenovateDashboard.App.Tests/Services/GitLabServiceTests.cs
git commit -m "feat: implement GitLabService with real API calls and filtering"
```

---

## Task 6: Wire up configuration and endpoints

**Files:**
- Modify: `src/RenovateDashboard.App/appsettings.json`
- Modify: `src/RenovateDashboard.App/Program.cs`
- Modify: `src/RenovateDashboard.App/Endpoints/MrEndpoints.cs`

- [ ] **Step 1: Add `GitLab` section to `appsettings.json`**

Replace the content of `src/RenovateDashboard.App/appsettings.json` with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GitLab": {
    "Token": "",
    "Url": "https://gitlab.com",
    "Repos": ""
  }
}
```

- [ ] **Step 2: Update `Program.cs` to bind `GitLabOptions`**

Replace the content of `src/RenovateDashboard.App/Program.cs` with:

```csharp
using RenovateDashboard.App.Endpoints;
using RenovateDashboard.App.Services;
using RenovateDashboard.App.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GitLabOptions>(builder.Configuration.GetSection("GitLab"));
builder.Services.AddHttpClient<GitLabService>();
builder.Services.AddScoped<DigestService>();
builder.Services.AddHostedService<DigestWorker>();

var app = builder.Build();

app.MapMrEndpoints();

app.Run();
```

- [ ] **Step 3: Pass `CancellationToken` in the MR endpoint**

Replace the content of `src/RenovateDashboard.App/Endpoints/MrEndpoints.cs` with:

```csharp
using RenovateDashboard.App.Services;

namespace RenovateDashboard.App.Endpoints;

public static class MrEndpoints
{
    public static IEndpointRouteBuilder MapMrEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        app.MapGet("/api/mrs", async (GitLabService gitLab, CancellationToken ct) =>
            Results.Ok(await gitLab.GetRenovateMrsAsync(ct)));

        app.MapPost("/api/digest/send", async (DigestService digest) =>
        {
            await digest.SendAsync();
            return Results.NoContent();
        });

        return app;
    }
}
```

- [ ] **Step 4: Build the full solution**

```bash
dotnet build RenovateDashboard.slnx
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Run all tests**

```bash
dotnet test RenovateDashboard.slnx
```

Expected: all tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/RenovateDashboard.App/appsettings.json \
        src/RenovateDashboard.App/Program.cs \
        src/RenovateDashboard.App/Endpoints/MrEndpoints.cs
git commit -m "feat: wire up GitLabOptions config binding and cancellation token"
```

---

## Task 7: Smoke test

**Goal:** Verify the API returns real Renovate MRs when run locally with valid credentials.

- [ ] **Step 1: Fill in real credentials in `appsettings.Development.json`**

Edit `src/RenovateDashboard.App/appsettings.Development.json` — replace placeholder values with your actual GitLab PAT and repo slugs:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "GitLab": {
    "Token": "<your-gitlab-pat>",
    "Url": "https://gitlab.com",
    "Repos": "namespace/repo1,namespace/repo2"
  }
}
```

The PAT needs `read_api` scope.

- [ ] **Step 2: Run the API locally**

```bash
dotnet run --project src/RenovateDashboard.App/
```

Expected: `Now listening on: http://localhost:5000` (or similar port from `launchSettings.json`).

- [ ] **Step 3: Call the MRs endpoint**

In a second terminal:

```bash
curl -s http://localhost:5000/api/mrs | jq .
```

Expected: a JSON array of objects each containing `repo`, `iid`, `title`, `webUrl`, `createdAt`, `author`, `sourceBranch`, `targetBranch`, `pipelineStatus`. Empty array `[]` is valid if no Renovate MRs are open.

- [ ] **Step 4: Stop the server**

Press `Ctrl+C` in the first terminal.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RenovateDashboard.App.Services;
using System.Net;
using System.Text.Json;

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
            new { iid = 1, title = "chore(deps,renovate): update lodash", web_url = "https://gitlab.example.com/ns/repo/-/merge_requests/1", created_at = "2026-04-01T10:00:00Z", author = new { name = "renovate-bot" }, source_branch = "renovate/lodash", target_branch = "main", head_pipeline = (object?)new { status = "success" } },
            new { iid = 2, title = "feat: unrelated MR", web_url = "https://gitlab.example.com/ns/repo/-/merge_requests/2", created_at = "2026-04-01T11:00:00Z", author = new { name = "dev" }, source_branch = "feat/x", target_branch = "main", head_pipeline = (object?)null }
        });

        var dict = new Dictionary<string, (HttpStatusCode, string)>
        {
            { MrUrl("ns/repo"), (HttpStatusCode.OK, body) }
        };
        var service = CreateService("ns/repo", dict);

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

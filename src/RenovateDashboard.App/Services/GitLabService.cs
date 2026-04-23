using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                response.Content.ReadAsStream(ct), JsonOpts, ct) ?? [];

            return mrs
                .Select(mr => new RenovateMrDto(
                    Repo: repo,
                    Iid: mr.Iid,
                    Title: mr.Title,
                    WebUrl: mr.WebUrl,
                    CreatedAt: mr.CreatedAt,
                    Author: mr.Author?.Name ?? "",
                    SourceBranch: mr.SourceBranch,
                    TargetBranch: mr.TargetBranch,
                    PipelineStatus: mr.MergeStatus
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

        [JsonPropertyName("merge_status")]
        public string MergeStatus { get; set; } = "";
    }

    private sealed class GitLabAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }
}

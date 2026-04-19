// src/RenovateDashboard.App/Services/GitLabService.cs
namespace RenovateDashboard.App.Services;

public class GitLabService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public GitLabService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public Task<IEnumerable<object>> GetRenovateMrsAsync() =>
        Task.FromResult(Enumerable.Empty<object>());
}

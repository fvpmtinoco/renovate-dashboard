// src/RenovateDashboard.App/Services/GitLabService.cs
namespace RenovateDashboard.App.Services;

public class GitLabService
{
    private readonly HttpClient _http;

    public GitLabService(HttpClient http)
    {
        _http = http;
    }

    public Task<IEnumerable<object>> GetRenovateMrsAsync() =>
        Task.FromResult(Enumerable.Empty<object>());
}

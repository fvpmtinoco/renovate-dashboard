namespace RenovateDashboard.App.Services;

public class GitLabOptions
{
    public string Token { get; set; } = "";
    public string Url { get; set; } = "https://gitlab.com";
    public string Repos { get; set; } = "";

    public IEnumerable<string> RepoList =>
        Repos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

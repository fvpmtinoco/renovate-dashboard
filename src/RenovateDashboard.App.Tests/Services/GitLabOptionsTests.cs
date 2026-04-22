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

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

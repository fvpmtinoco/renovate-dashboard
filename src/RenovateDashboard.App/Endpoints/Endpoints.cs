using RenovateDashboard.App.Services;

namespace RenovateDashboard.App.Endpoints;

public static class Endpoints
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () =>
            Results.Ok(
            new
            {
                status = "healthy"
            }));

        app.MapGet("/api/mrs", async (GitLabService gitLab, CancellationToken ct) =>
            Results.Ok(
                await gitLab.GetRenovateMrsAsync(ct))
            ).RequireAuthorization();

        return app;
    }
}

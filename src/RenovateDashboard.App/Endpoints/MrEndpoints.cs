using RenovateDashboard.App.Services;

namespace RenovateDashboard.App.Endpoints;

public static class MrEndpoints
{
    public static IEndpointRouteBuilder MapMrEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        app.MapGet("/api/mrs", async (GitLabService gitLab, CancellationToken ct) =>
            Results.Ok(await gitLab.GetRenovateMrsAsync(ct)))
            .RequireAuthorization();

        app.MapPost("/api/digest/send", async (DigestService digest) =>
        {
            await digest.SendAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }
}

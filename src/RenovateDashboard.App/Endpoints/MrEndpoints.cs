// src/RenovateDashboard.App/Endpoints/MrEndpoints.cs
using RenovateDashboard.App.Services;

namespace RenovateDashboard.App.Endpoints;

public static class MrEndpoints
{
    public static IEndpointRouteBuilder MapMrEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

        app.MapGet("/api/mrs", async (GitLabService gitLab) =>
            Results.Ok(await gitLab.GetRenovateMrsAsync()));

        app.MapPost("/api/digest/send", async (DigestService digest) =>
        {
            await digest.SendAsync();
            return Results.NoContent();
        });

        return app;
    }
}

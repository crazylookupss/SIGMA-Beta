using Microsoft.AspNetCore.Http.HttpResults;

namespace SIGMA.Api.Endpoints;

internal static class HealthEndpoint
{
    public static RouteGroupBuilder MapHealthEndpoint(this RouteGroupBuilder group)
    {
        group.MapGet("/health", () =>
        {
            return Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
        })
        .AllowAnonymous()
        .WithName("HealthCheck")
        .WithTags("Health");

        return group;
    }
}

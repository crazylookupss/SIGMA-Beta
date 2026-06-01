using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace SIGMA.Api.Endpoints;

internal static class HealthEndpoint
{
    public static RouteGroupBuilder MapHealthEndpoint(this RouteGroupBuilder group)
    {
        group.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTimeOffset.UtcNow,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = e.Value.Duration.TotalMilliseconds,
                        description = e.Value.Description,
                    }),
                };
                await context.Response.WriteAsJsonAsync(result);
            }
        });

        return group;
    }
}

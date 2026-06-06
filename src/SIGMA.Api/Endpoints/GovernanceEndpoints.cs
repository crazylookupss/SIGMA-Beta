using SIGMA.Application.Governance;
using SIGMA.Application.Governance.Models;

namespace SIGMA.Api.Endpoints;

internal static class GovernanceEndpoints
{
    public static RouteGroupBuilder MapGovernanceEndpoints(this RouteGroupBuilder group, int cacheSeconds = 0)
    {
        group.MapGet("/governance/findings", async (
            bool? force,
            IGovernanceFindingService service,
            CancellationToken ct) =>
        {
            var result = await service.GetFindingsAsync(force ?? false, ct);
            return Results.Ok(result);
        })
        .WithName("GetGovernanceFindings")
        .WithTags("Governance")
        .WithSummary("Get all governance findings")
        .WithDescription("Returns aggregated governance findings across applications, groups, and users.")
        .ConfigureOutputCache(cacheSeconds);

        group.MapGet("/governance/findings/summary", async (
            bool? force,
            IGovernanceFindingService service,
            CancellationToken ct) =>
        {
            var result = await service.GetFindingsAsync(force ?? false, ct);
            return Results.Ok(result.Summary);
        })
        .WithName("GetGovernanceSummary")
        .WithTags("Governance")
        .WithSummary("Get governance findings summary")
        .WithDescription("Returns summary counts of governance findings by severity.")
        .ConfigureOutputCache(cacheSeconds);

        group.MapGet("/governance/findings/category/{category}", async (
            string category,
            IGovernanceFindingService service,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<FindingCategory>(category, true, out var parsedCategory))
                return Results.BadRequest(new { error = $"Invalid category: {category}" });

            var result = await service.GetFindingsByCategoryAsync(parsedCategory, ct);
            return Results.Ok(result);
        })
        .WithName("GetGovernanceFindingsByCategory")
        .WithTags("Governance")
        .WithSummary("Get findings by category")
        .WithDescription("Returns governance findings filtered by category.")
        .ConfigureOutputCache(cacheSeconds);

        group.MapGet("/governance/findings/severity/{severity}", async (
            string severity,
            IGovernanceFindingService service,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<FindingSeverity>(severity, true, out var parsedSeverity))
                return Results.BadRequest(new { error = $"Invalid severity: {severity}" });

            var result = await service.GetFindingsBySeverityAsync(parsedSeverity, ct);
            return Results.Ok(result);
        })
        .WithName("GetGovernanceFindingsBySeverity")
        .WithTags("Governance")
        .WithSummary("Get findings by severity")
        .WithDescription("Returns governance findings filtered by severity.")
        .ConfigureOutputCache(cacheSeconds);

        return group;
    }

    private static RouteHandlerBuilder ConfigureOutputCache(this RouteHandlerBuilder builder, int cacheSeconds)
    {
        if (cacheSeconds > 0)
            builder.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(cacheSeconds)));
        return builder;
    }
}

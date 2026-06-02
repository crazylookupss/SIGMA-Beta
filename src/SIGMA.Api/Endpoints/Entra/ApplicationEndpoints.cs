using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.Applications.GetApplication;
using SIGMA.Application.Features.Entra.Applications.ListApplications;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints.Entra;

internal static class ApplicationEndpoints
{
    public static RouteGroupBuilder MapApplicationEndpoints(this RouteGroupBuilder group)
    {
        // NOTE: statistics route must be registered BEFORE {id} to avoid route conflict
        group.MapGet("/applications/statistics", async (
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var stats = await graph.GetApplicationStatisticsAsync(ct);
            return Results.Ok(new { data = stats });
        })
        .WithName("GetApplicationStatistics")
        .CacheOutput(options => options.Expire(TimeSpan.FromMinutes(2)).Tag("applications"))
        .WithTags("Entra Applications")
        .WithSummary("Get aggregated App Registration statistics")
        .WithDescription("Returns total counts, status distribution, protocol distribution, and risk metrics for all App Registrations.");

        group.MapGet("/applications", async (
            [AsParameters] ListApplicationsQuery query,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("ListApplications")
        .CacheOutput(options => options.Expire(TimeSpan.FromSeconds(60)).Tag("applications"))
        .WithTags("Entra Applications")
        .WithSummary("List all Entra ID applications")
        .WithDescription("Returns a paginated list of App Registrations from Microsoft Entra ID.");

        group.MapGet("/applications/{id}", async (
            string id,
            string? select,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var query = new GetApplicationQuery(id, select);
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetApplication")
        .WithTags("Entra Applications")
        .WithSummary("Get Entra ID application by ID")
        .WithDescription("Returns details of a specific App Registration from Microsoft Entra ID.");

        group.MapGet("/applications/{id}/owners", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var owners = await graph.GetApplicationOwnersAsync(id, ct);
            return Results.Ok(new { data = owners });
        })
        .WithName("GetApplicationOwners")
        .WithTags("Entra Applications")
        .WithSummary("Get owners of an App Registration")
        .WithDescription("Returns the owners (users and service principals) of this application registration.");

        group.MapGet("/applications/{id}/service-principals", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var appResult = await graph.GetApplicationByIdAsync(id, "appId", ct);
            if (appResult.IsFailure || appResult.Value?.AppId == null)
                return Results.Ok(new { data = new List<ServicePrincipalRef>() });

            var sps = await graph.GetServicePrincipalsForApplicationAsync(appResult.Value.AppId, ct);
            return Results.Ok(new { data = sps });
        })
        .WithName("GetApplicationServicePrincipals")
        .WithTags("Entra Applications")
        .WithSummary("Get linked service principals for an App Registration")
        .WithDescription("Returns all enterprise applications (service principals) linked to this app registration.");

        group.MapGet("/applications/{id}/credentials", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var creds = await graph.GetApplicationCredentialsAsync(id, ct);
            return Results.Ok(new { data = creds });
        })
        .WithName("GetApplicationCredentials")
        .WithTags("Entra Applications")
        .WithSummary("Get certificate and secret health for an App Registration")
        .WithDescription("Returns key credentials and password credentials with expiry status and risk assessment.");

        group.MapGet("/applications/{id}/permissions", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var appResult = await graph.GetApplicationByIdAsync(id, "appId", ct);
            if (appResult.IsFailure || appResult.Value?.AppId == null)
                return Results.Ok(new { data = new List<EntraAppPermission>() });

            var detailsResult = await graph.GetApplicationByAppIdAsync(appResult.Value.AppId, ct);
            if (detailsResult.IsFailure || detailsResult.Value is null)
                return Results.Ok(new { data = new List<EntraAppPermission>() });
            return Results.Ok(new { data = detailsResult.Value.RequiredResourceAccess });
        })
        .WithName("GetApplicationPermissions")
        .WithTags("Entra Applications")
        .WithSummary("Get API permissions for an App Registration")
        .WithDescription("Returns all required resource access (delegated and application permissions) for this app registration.");

        group.MapGet("/applications/{id}/signins", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var appResult = await graph.GetApplicationByIdAsync(id, "appId", ct);
            if (appResult.IsFailure || appResult.Value?.AppId == null)
                return Results.Ok(new { data = new List<SignInHistoryEntry>() });

            var signIns = await graph.GetSignInsForServicePrincipalAsync(appResult.Value.AppId, 7, ct);
            return Results.Ok(new { data = signIns });
        })
        .WithName("GetApplicationSignIns")
        .WithTags("Entra Applications")
        .WithSummary("Get recent sign-in activity for an App Registration")
        .WithDescription("Returns recent sign-in events for this application (requires P1/P2 license).");

        group.MapGet("/applications/{id}/audit-logs", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var appResult = await graph.GetApplicationByIdAsync(id, "appId", ct);
            if (appResult.IsFailure || appResult.Value?.AppId == null)
                return Results.Ok(new { data = new List<AuditLogEntry>() });

            var logs = await graph.GetApplicationAuditLogsAsync(appResult.Value.AppId, 50, ct);
            return Results.Ok(new { data = logs });
        })
        .WithName("GetApplicationAuditLogs")
        .WithTags("Entra Applications")
        .WithSummary("Get audit log entries for an App Registration")
        .WithDescription("Returns recent audit/sign-in log entries for this application.");

        group.MapGet("/applications/{id}/manifest", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var result = await graph.GetApplicationManifestAsync(id, ct);
            if (result.IsSuccess)
                return Results.Content(result.Value!, "application/json");
            return Results.Ok(new { data = result.Value });
        })
        .WithName("GetApplicationManifest")
        .WithTags("Entra Applications")
        .WithSummary("Get the manifest of an App Registration")
        .WithDescription("Returns the raw JSON manifest for this application.");

        group.MapGet("/applications/{id}/service-principal-ref", async (
            string id,
            IGraphClientService graph,
            CancellationToken ct) =>
        {
            var appResult = await graph.GetApplicationByIdAsync(id, "appId", ct);
            if (appResult.IsFailure || appResult.Value?.AppId == null)
                return Results.Ok(new { data = (ServicePrincipalRef?)null });

            var spResult = await graph.GetServicePrincipalForApplicationAsync(appResult.Value.AppId, ct);
            return Results.Ok(new { data = spResult.IsSuccess ? spResult.Value : null });
        })
        .WithName("GetApplicationServicePrincipalRef")
        .WithTags("Entra Applications")
        .WithSummary("Get the primary service principal for an App Registration")
        .WithDescription("Returns the first matching enterprise application (service principal) linked to this app registration.");

        return group;
    }
}

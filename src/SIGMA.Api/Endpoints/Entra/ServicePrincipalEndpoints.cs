using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipal;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalSsoConfig;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalProxyConfig;
using SIGMA.Application.Features.Entra.ServicePrincipals.ListServicePrincipals;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalDashboard;
using SIGMA.Application.Features.ProtocolAnalysis.Queries.GetProtocolAnalysis;
using SIGMA.Api.Middleware;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints.Entra;

internal static class ServicePrincipalEndpoints
{
    public static RouteGroupBuilder MapServicePrincipalEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/service-principals/dashboard", async (
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetServicePrincipalDashboardQuery(), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetServicePrincipalDashboard")
        .WithTags("Entra Service Principals")
        .WithSummary("Get Enterprise Applications dashboard analytics")
        .WithDescription("Returns aggregated counts, donut status segments, and 30 days sign-ins line series.");

        group.MapGet("/service-principals", async (

            [AsParameters] ListServicePrincipalsQuery query,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("ListServicePrincipals")
        .WithTags("Entra Service Principals")
        .WithSummary("List all Entra ID service principals / enterprise apps")
        .WithDescription("Returns a paginated list of service principals (enterprise applications) from Microsoft Entra ID.")
        .CacheOutput(builder => builder.Expire(TimeSpan.FromSeconds(60)));

        group.MapGet("/service-principals/{id}", async (
            string id,
            string? select,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var query = new GetServicePrincipalQuery(id, select);
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetServicePrincipal")
        .WithTags("Entra Service Principals")
        .WithSummary("Get Entra ID service principal by ID")
        .WithDescription("Returns details of a specific service principal from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/service-principals/{id}/application", async (
            string id,
            IGraphClientService graphClient,
            CancellationToken ct) =>
        {
            // First get the service principal to find its appId
            var spResult = await graphClient.GetServicePrincipalByIdAsync(id, null, ct);
            if (spResult.IsFailure || spResult.Value?.AppId == null)
                return Results.NotFound(new { title = "Not Found", status = 404 });

            var appResult = await graphClient.GetApplicationByAppIdAsync(spResult.Value.AppId, ct);
            return ResultMapper.ToResult(appResult);
        })
        .WithName("GetServicePrincipalApplication")
        .WithTags("Entra Service Principals")
        .WithSummary("Get the linked application registration for a service principal")
        .WithDescription("Returns the Microsoft Entra ID application registration linked to this enterprise application.");

        group.MapGet("/service-principals/{id}/assignments", async (
            string id,
            IGraphClientService graphClient,
            CancellationToken ct) =>
        {
            var assignments = await graphClient.GetServicePrincipalAssignmentsAsync(id, ct);
            return Results.Ok(new { data = assignments });
        })
        .WithName("GetServicePrincipalAssignments")
        .WithTags("Entra Service Principals")
        .WithSummary("Get user/group assignments for a service principal")
        .WithDescription("Returns a list of users and groups assigned to this enterprise application.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/service-principals/{id}/owners", async (
            string id,
            IGraphClientService graphClient,
            CancellationToken ct) =>
        {
            var owners = await graphClient.GetServicePrincipalOwnersAsync(id, ct);
            return Results.Ok(new { data = owners });
        })
        .WithName("GetServicePrincipalOwners")
        .WithTags("Entra Service Principals")
        .WithSummary("Get owners of a service principal")
        .WithDescription("Returns the owners of this enterprise application.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/service-principals/{id}/sso-config", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetServicePrincipalSsoConfigQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetServicePrincipalSsoConfig")
        .WithTags("Entra Service Principals")
        .WithSummary("Get SSO configuration for a service principal")
        .WithDescription("Returns SAML/OIDC configuration including metadata URLs, certificates, reply URLs, and tenant endpoints for the linked application registration.");

        group.MapGet("/service-principals/{id}/proxy-configuration", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetServicePrincipalProxyConfigQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetServicePrincipalProxyConfiguration")
        .WithTags("Entra Service Principals")
        .WithSummary("Get Application Proxy configuration for a service principal")
        .WithDescription("Returns proxy tunnel status, internal/external URLs, pre-authentication settings, and URL translation configuration.");

        group.MapGet("/service-principals/{id}/protocol-analysis", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetProtocolAnalysisQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetServicePrincipalProtocolAnalysis")
        .WithTags("Entra Service Principals")
        .WithSummary("Detect SSO/federation protocols for a service principal")
        .WithDescription("Performs weighted evidence-based protocol analysis to detect supported SSO protocols (SAML, OIDC, OAuth 2.0, WS-Federation, Header-Based, Password SSO, Linked Sign-On, SCIM Provisioning) with confidence scoring.");

        group.MapGet("/service-principals/{id}/signins", async (
            string id,
            IGraphClientService graphClient,
            CancellationToken ct) =>
        {
            // First get the service principal to find its appId
            var spResult = await graphClient.GetServicePrincipalByIdAsync(id, null, ct);
            if (spResult.IsFailure || spResult.Value?.AppId == null)
                return Results.Ok(new { data = new List<object>() });

            var signIns = await graphClient.GetSignInsForServicePrincipalAsync(spResult.Value.AppId, 7, ct);
            return Results.Ok(new { data = signIns });
        })
        .WithName("GetServicePrincipalSignIns")
        .WithTags("Entra Service Principals")
        .WithSummary("Get sign-in activity for a service principal")
        .WithDescription("Returns recent sign-in activity for this enterprise application (requires P1/P2 license).");

        return group;
    }
}

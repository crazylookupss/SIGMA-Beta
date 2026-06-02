using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.Groups.GetGroup;
using SIGMA.Application.Features.Entra.Groups.GetMembers;
using SIGMA.Application.Features.Entra.Groups.GetOwners;
using SIGMA.Application.Features.Entra.Groups.GetApplications;
using SIGMA.Application.Features.Entra.Groups.GetDevices;
using SIGMA.Application.Features.Entra.Groups.GetAuditLogs;
using SIGMA.Application.Features.Entra.Groups.GetAccessReviews;
using SIGMA.Application.Features.Entra.Groups.ListGroups;
using SIGMA.Api.Middleware;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints.Entra;

internal static class GroupEndpoints
{
    public static RouteGroupBuilder MapGroupEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/groups", async (
            [AsParameters] ListGroupsQuery query,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("ListGroups")
        .CacheOutput(options => options.Expire(TimeSpan.FromSeconds(60)).Tag("groups"))
        .WithTags("Entra Groups")
        .WithSummary("List all Entra ID groups")
        .WithDescription("Returns a paginated list of groups from Microsoft Entra ID.");

        group.MapGet("/groups/{id}", async (
            string id,
            string? select,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var query = new GetGroupQuery(id, select);
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetGroup")
        .WithTags("Entra Groups")
        .WithSummary("Get Entra ID group by ID")
        .WithDescription("Returns details of a specific group from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/groups/{id}/members", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetGroupMembersQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetGroupMembers")
        .WithTags("Entra Groups")
        .WithSummary("Get group members")
        .WithDescription("Returns the members of a specific group from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/groups/{id}/owners", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetGroupOwnersQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetGroupOwners")
        .WithTags("Entra Groups")
        .WithSummary("Get group owners")
        .WithDescription("Returns the owners of a specific group from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/groups/{id}/applications", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetGroupApplicationsQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetGroupApplications")
        .WithTags("Entra Groups")
        .WithSummary("Get group app role assignments")
        .WithDescription("Returns the application role assignments for a specific group from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/groups/{id}/devices", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetGroupDevicesQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetGroupDevices")
        .WithTags("Entra Groups")
        .WithSummary("Get group devices")
        .WithDescription("Returns the device members of a specific group from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/groups/{id}/audit-logs", async (
            string id,
            int? top,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetGroupAuditLogsQuery(id, top), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetGroupAuditLogs")
        .WithTags("Entra Groups")
        .WithSummary("Get group audit logs")
        .WithDescription("Returns audit log entries for a specific group from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        group.MapGet("/groups/{id}/access-reviews", async (
            string id,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetGroupAccessReviewsQuery(id), ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetGroupAccessReviews")
        .WithTags("Entra Groups")
        .WithSummary("Get group access reviews")
        .WithDescription("Returns access review definitions for a specific group from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        return group;
    }
}

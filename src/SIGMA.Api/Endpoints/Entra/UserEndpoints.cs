using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.Users.GetUser;
using SIGMA.Application.Features.Entra.Users.ListUsers;
using SIGMA.Api.Middleware;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints.Entra;

internal static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/users", async (
            [AsParameters] ListUsersQuery query,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("ListUsers")
        .CacheOutput(options => options.Expire(TimeSpan.FromSeconds(60)).Tag("users"))
        .WithTags("Entra Users")
        .WithSummary("List all Entra ID users")
        .WithDescription("Returns a paginated list of users from Microsoft Entra ID.");

        group.MapGet("/users/{id}", async (
            string id,
            string? select,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var query = new GetUserQuery(id, select);
            var result = await dispatcher.Send(query, ct);
            return ResultMapper.ToResult(result);
        })
        .WithName("GetUser")
        .WithTags("Entra Users")
        .WithSummary("Get Entra ID user by ID")
        .WithDescription("Returns details of a specific user from Microsoft Entra ID.")
        .WithMetadata(new CachedAttribute(300));

        return group;
    }
}

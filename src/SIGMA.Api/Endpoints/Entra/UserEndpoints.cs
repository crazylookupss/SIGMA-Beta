using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.Users.GetUser;
using SIGMA.Application.Features.Entra.Users.ListUsers;
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
            return ToResult(result);
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
            return ToResult(result);
        })
        .WithName("GetUser")
        .WithTags("Entra Users")
        .WithSummary("Get Entra ID user by ID")
        .WithDescription("Returns details of a specific user from Microsoft Entra ID.");

        return group;
    }

    private static IResult ToResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return result.Value is null
                ? Results.NotFound(new { title = "Not Found", status = 404 })
                : Results.Ok(new { data = result.Value });

        return result.Error!.Type switch
        {
            ErrorType.NotFound => Results.NotFound(new
            {
                type = "https://tools.ietf.org/html/rfc9457",
                title = result.Error.Code,
                status = 404,
                detail = result.Error.Description,
            }),
            ErrorType.Validation => Results.BadRequest(new
            {
                type = "https://tools.ietf.org/html/rfc9457",
                title = result.Error.Code,
                status = 400,
                detail = result.Error.Description,
            }),
            ErrorType.ExternalService => Results.StatusCode(502),
            _ => Results.Problem(
                title: result.Error.Code,
                detail: result.Error.Description,
                statusCode: 500),
        };
    }
}

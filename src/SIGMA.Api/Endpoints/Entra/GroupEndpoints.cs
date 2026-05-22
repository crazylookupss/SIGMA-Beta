using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.Groups.GetGroup;
using SIGMA.Application.Features.Entra.Groups.ListGroups;
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
            return ToResult(result);
        })
        .WithName("ListGroups")
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
            return ToResult(result);
        })
        .WithName("GetGroup")
        .WithTags("Entra Groups")
        .WithSummary("Get Entra ID group by ID")
        .WithDescription("Returns details of a specific group from Microsoft Entra ID.");

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

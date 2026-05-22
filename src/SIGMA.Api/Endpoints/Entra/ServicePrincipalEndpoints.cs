using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipal;
using SIGMA.Application.Features.Entra.ServicePrincipals.ListServicePrincipals;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints.Entra;

internal static class ServicePrincipalEndpoints
{
    public static RouteGroupBuilder MapServicePrincipalEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/service-principals", async (
            [AsParameters] ListServicePrincipalsQuery query,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(query, ct);
            return ToResult(result);
        })
        .WithName("ListServicePrincipals")
        .WithTags("Entra Service Principals")
        .WithSummary("List all Entra ID service principals / enterprise apps")
        .WithDescription("Returns a paginated list of service principals (enterprise applications) from Microsoft Entra ID.");

        group.MapGet("/service-principals/{id}", async (
            string id,
            string? select,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var query = new GetServicePrincipalQuery(id, select);
            var result = await dispatcher.Send(query, ct);
            return ToResult(result);
        })
        .WithName("GetServicePrincipal")
        .WithTags("Entra Service Principals")
        .WithSummary("Get Entra ID service principal by ID")
        .WithDescription("Returns details of a specific service principal from Microsoft Entra ID.");

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

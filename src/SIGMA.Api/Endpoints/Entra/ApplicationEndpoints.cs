using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.Applications.GetApplication;
using SIGMA.Application.Features.Entra.Applications.ListApplications;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints.Entra;

internal static class ApplicationEndpoints
{
    public static RouteGroupBuilder MapApplicationEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/applications", async (
            [AsParameters] ListApplicationsQuery query,
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(query, ct);
            return ToResult(result);
        })
        .WithName("ListApplications")
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
            return ToResult(result);
        })
        .WithName("GetApplication")
        .WithTags("Entra Applications")
        .WithSummary("Get Entra ID application by ID")
        .WithDescription("Returns details of a specific App Registration from Microsoft Entra ID.");

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

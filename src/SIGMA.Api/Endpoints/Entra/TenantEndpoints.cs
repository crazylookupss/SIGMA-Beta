using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.Entra.Tenant;
using SIGMA.Domain.Common;

namespace SIGMA.Api.Endpoints.Entra;

internal static class TenantEndpoints
{
    public static RouteGroupBuilder MapTenantEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/tenant", async (
            IQueryDispatcher dispatcher,
            CancellationToken ct) =>
        {
            var result = await dispatcher.Send(new GetTenantQuery(), ct);

            if (result.IsSuccess)
                return Results.Ok(new { data = result.Value });

            return Results.Problem(
                title: result.Error!.Code,
                detail: result.Error.Description,
                statusCode: 500);
        })
        .WithName("GetTenantDetails")
        .WithTags("Entra Tenant")
        .WithSummary("Get active Entra ID tenant connection metadata")
        .WithDescription("Returns live organization info and direct counts from Microsoft Entra ID.");

        return group;
    }
}

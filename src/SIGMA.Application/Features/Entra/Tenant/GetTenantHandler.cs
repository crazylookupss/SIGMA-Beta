using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Tenant;

internal sealed class GetTenantHandler(IGraphClientService graphClient)
    : IQueryHandler<GetTenantQuery, Result<GetTenantResponse>>
{
    public async Task<Result<GetTenantResponse>> Handle(GetTenantQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetTenantDetailsAsync(cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var tenant = result.Value!;
        return Result.Success(new GetTenantResponse
        {
            Id = tenant.Id,
            DisplayName = tenant.DisplayName,
            PrimaryDomain = tenant.PrimaryDomain,
            License = tenant.License,
            UsersCount = tenant.UsersCount,
            GroupsCount = tenant.GroupsCount,
            ApplicationsCount = tenant.ApplicationsCount,
            EnterpriseApplicationsCount = tenant.EnterpriseApplicationsCount,
            DevicesCount = tenant.DevicesCount
        });
    }
}

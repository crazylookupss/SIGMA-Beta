using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipal;

internal sealed class GetServicePrincipalHandler(IGraphClientService graphClient)
    : IQueryHandler<GetServicePrincipalQuery, Result<GetServicePrincipalResponse>>
{
    public async Task<Result<GetServicePrincipalResponse>> Handle(
        GetServicePrincipalQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetServicePrincipalByIdAsync(
            query.Id, query.Select, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var sp = result.Value!;
        var response = new GetServicePrincipalResponse
        {
            Id = sp.Id,
            AppId = sp.AppId,
            DisplayName = sp.DisplayName,
            AppDisplayName = sp.AppDisplayName,
            ServicePrincipalType = sp.ServicePrincipalType,
            AccountEnabled = sp.AccountEnabled,
            PublisherName = sp.PublisherName,
            SignInAudience = sp.SignInAudience,
            Tags = sp.Tags,
            AppOwnerOrganizationId = sp.AppOwnerOrganizationId,
            CreatedDateTime = sp.CreatedDateTime,
        };

        return Result.Success(response);
    }
}

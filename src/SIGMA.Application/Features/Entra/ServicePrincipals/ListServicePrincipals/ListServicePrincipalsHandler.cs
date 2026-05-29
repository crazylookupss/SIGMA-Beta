using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.ListServicePrincipals;

internal sealed class ListServicePrincipalsHandler(IGraphClientService graphClient)
    : IQueryHandler<ListServicePrincipalsQuery, Result<PagedResponse<ListServicePrincipalsResponse>>>
{
    public async Task<Result<PagedResponse<ListServicePrincipalsResponse>>> Handle(
        ListServicePrincipalsQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetServicePrincipalsAsync(
            query.Select, query.Filter, query.Top, query.Skip, query.Count,
            cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var response = new PagedResponse<ListServicePrincipalsResponse>
        {
            Data = result.Value!.Data
                .Select(sp => new ListServicePrincipalsResponse
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
                    SignInStatus = sp.SignInStatus,
                    UsersCount = sp.UsersCount,
                    LastSignIn = sp.LastSignIn,
                    AppRoleAssignmentRequired = sp.AppRoleAssignmentRequired,
                    PreferredSingleSignOnMode = sp.PreferredSingleSignOnMode
                })
                .ToList(),

            NextLink = result.Value.NextLink,
            Count = result.Value.Count,
        };

        return Result.Success(response);
    }
}

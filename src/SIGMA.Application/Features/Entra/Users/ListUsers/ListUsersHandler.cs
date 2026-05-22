using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Users.ListUsers;

internal sealed class ListUsersHandler(IGraphClientService graphClient)
    : IQueryHandler<ListUsersQuery, Result<PagedResponse<ListUsersResponse>>>
{
    public async Task<Result<PagedResponse<ListUsersResponse>>> Handle(
        ListUsersQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetUsersAsync(
            query.Select, query.Filter, query.Top, query.Skip, query.Count,
            cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var response = new PagedResponse<ListUsersResponse>
        {
            Data = result.Value!.Data
                .Select(u => new ListUsersResponse
                {
                    Id = u.Id,
                    DisplayName = u.DisplayName,
                    UserPrincipalName = u.UserPrincipalName,
                    GivenName = u.GivenName,
                    Surname = u.Surname,
                    JobTitle = u.JobTitle,
                    Mail = u.Mail,
                    MobilePhone = u.MobilePhone,
                    OfficeLocation = u.OfficeLocation,
                    PreferredLanguage = u.PreferredLanguage,
                    BusinessPhone = u.BusinessPhone,
                    AccountEnabled = u.AccountEnabled,
                    UserType = u.UserType,
                })
                .ToList(),
            NextLink = result.Value.NextLink,
            Count = result.Value.Count,
        };

        return Result.Success(response);
    }
}

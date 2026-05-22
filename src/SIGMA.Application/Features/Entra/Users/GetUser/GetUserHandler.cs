using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Users.GetUser;

internal sealed class GetUserHandler(IGraphClientService graphClient)
    : IQueryHandler<GetUserQuery, Result<GetUserResponse>>
{
    public async Task<Result<GetUserResponse>> Handle(
        GetUserQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetUserByIdAsync(
            query.Id, query.Select, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var user = result.Value!;
        var response = new GetUserResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            UserPrincipalName = user.UserPrincipalName,
            GivenName = user.GivenName,
            Surname = user.Surname,
            JobTitle = user.JobTitle,
            Mail = user.Mail,
            MobilePhone = user.MobilePhone,
            OfficeLocation = user.OfficeLocation,
            PreferredLanguage = user.PreferredLanguage,
            BusinessPhone = user.BusinessPhone,
            AccountEnabled = user.AccountEnabled,
            UserType = user.UserType,
        };

        return Result.Success(response);
    }
}

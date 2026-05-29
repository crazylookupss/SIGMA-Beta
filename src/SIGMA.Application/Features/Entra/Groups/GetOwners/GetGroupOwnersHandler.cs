using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetOwners;

internal sealed class GetGroupOwnersHandler(IGraphClientService graphClient)
    : IQueryHandler<GetGroupOwnersQuery, Result<PagedResponse<GroupOwnerResponse>>>
{
    public async Task<Result<PagedResponse<GroupOwnerResponse>>> Handle(
        GetGroupOwnersQuery query, CancellationToken cancellationToken)
    {
        var owners = await graphClient.GetGroupOwnersAsync(query.Id, cancellationToken);
        return Result.Success(new PagedResponse<GroupOwnerResponse>
        {
            Data = owners.Select(o => new GroupOwnerResponse
            {
                Id = o.Id,
                DisplayName = o.DisplayName,
                UserPrincipalName = o.UserPrincipalName,
                OwnerType = o.OwnerType,
            }).ToList(),
            Count = owners.Count,
        });
    }
}

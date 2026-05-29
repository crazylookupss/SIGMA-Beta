using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetMembers;

internal sealed class GetGroupMembersHandler(IGraphClientService graphClient)
    : IQueryHandler<GetGroupMembersQuery, Result<PagedResponse<GroupMemberResponse>>>
{
    public async Task<Result<PagedResponse<GroupMemberResponse>>> Handle(
        GetGroupMembersQuery query, CancellationToken cancellationToken)
    {
        var members = await graphClient.GetGroupMembersAsync(query.Id, cancellationToken);
        return Result.Success(new PagedResponse<GroupMemberResponse>
        {
            Data = members.Select(m => new GroupMemberResponse
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                UserPrincipalName = m.UserPrincipalName,
                MemberType = m.MemberType,
                CreatedDateTime = m.CreatedDateTime,
            }).ToList(),
            Count = members.Count,
        });
    }
}

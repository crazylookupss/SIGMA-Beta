using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetGroup;

internal sealed class GetGroupHandler(IGraphClientService graphClient)
    : IQueryHandler<GetGroupQuery, Result<GetGroupResponse>>
{
    public async Task<Result<GetGroupResponse>> Handle(
        GetGroupQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetGroupByIdAsync(
            query.Id, query.Select, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var group = result.Value!;
        var response = new GetGroupResponse
        {
            Id = group.Id,
            DisplayName = group.DisplayName,
            Description = group.Description,
            Mail = group.Mail,
            MailEnabled = group.MailEnabled,
            SecurityEnabled = group.SecurityEnabled,
            MailNickname = group.MailNickname,
            GroupTypes = group.GroupTypes,
            Visibility = group.Visibility,
            CreatedDateTime = group.CreatedDateTime,
            MemberCount = group.MemberCount,

            // New enriched property mapping
            MembershipType = group.MembershipType,
            Source = group.Source,
            Type = group.Type,
            TotalDirectMembers = group.TotalDirectMembers,
            DirectUsers = group.DirectUsers,
            DirectGroups = group.DirectGroups,
            DirectDevices = group.DirectDevices,
            DirectOthers = group.DirectOthers,
            GroupMembershipsCount = group.GroupMembershipsCount,
            OwnersCount = group.OwnersCount,
            TotalMembers = group.TotalMembers,
        };

        return Result.Success(response);
    }
}

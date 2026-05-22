using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.ListGroups;

internal sealed class ListGroupsHandler(IGraphClientService graphClient)
    : IQueryHandler<ListGroupsQuery, Result<PagedResponse<ListGroupsResponse>>>
{
    public async Task<Result<PagedResponse<ListGroupsResponse>>> Handle(
        ListGroupsQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetGroupsAsync(
            query.Select, query.Filter, query.Top, query.Skip, query.Count,
            cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var response = new PagedResponse<ListGroupsResponse>
        {
            Data = result.Value!.Data
                .Select(g => new ListGroupsResponse
                {
                    Id = g.Id,
                    DisplayName = g.DisplayName,
                    Description = g.Description,
                    Mail = g.Mail,
                    MailEnabled = g.MailEnabled,
                    SecurityEnabled = g.SecurityEnabled,
                    MailNickname = g.MailNickname,
                    GroupTypes = g.GroupTypes,
                    Visibility = g.Visibility,
                    CreatedDateTime = g.CreatedDateTime,
                    MemberCount = g.MemberCount,
                    MembershipType = g.MembershipType,
                    Source = g.Source,
                    Type = g.Type,
                })
                .ToList(),
            NextLink = result.Value.NextLink,
            Count = result.Value.Count,
        };

        return Result.Success(response);
    }
}

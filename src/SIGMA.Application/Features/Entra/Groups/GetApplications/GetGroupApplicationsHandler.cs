using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetApplications;

internal sealed class GetGroupApplicationsHandler(IGraphClientService graphClient)
    : IQueryHandler<GetGroupApplicationsQuery, Result<PagedResponse<GroupApplicationResponse>>>
{
    public async Task<Result<PagedResponse<GroupApplicationResponse>>> Handle(
        GetGroupApplicationsQuery query, CancellationToken cancellationToken)
    {
        var apps = await graphClient.GetGroupAppRoleAssignmentsAsync(query.Id, cancellationToken);
        return Result.Success(new PagedResponse<GroupApplicationResponse>
        {
            Data = apps.Select(a => new GroupApplicationResponse
            {
                Id = a.Id,
                DisplayName = a.DisplayName,
                AppRoleId = a.AppRoleId,
                ResourceId = a.ResourceId,
                CreatedDateTime = a.CreatedDateTime,
            }).ToList(),
            Count = apps.Count,
        });
    }
}

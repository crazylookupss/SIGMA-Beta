using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetAccessReviews;

internal sealed class GetGroupAccessReviewsHandler(IGraphClientService graphClient)
    : IQueryHandler<GetGroupAccessReviewsQuery, Result<PagedResponse<GroupAccessReviewResponse>>>
{
    public async Task<Result<PagedResponse<GroupAccessReviewResponse>>> Handle(
        GetGroupAccessReviewsQuery query, CancellationToken cancellationToken)
    {
        var reviews = await graphClient.GetGroupAccessReviewsAsync(query.Id, cancellationToken);
        return Result.Success(new PagedResponse<GroupAccessReviewResponse>
        {
            Data = reviews.Select(r => new GroupAccessReviewResponse
            {
                Id = r.Id,
                DisplayName = r.DisplayName,
                Status = r.Status,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                ReviewersCount = r.ReviewersCount,
            }).ToList(),
            Count = reviews.Count,
        });
    }
}

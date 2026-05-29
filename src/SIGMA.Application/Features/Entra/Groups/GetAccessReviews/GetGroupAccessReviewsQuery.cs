using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetAccessReviews;

public sealed record GetGroupAccessReviewsQuery(string Id) : IQuery<Result<PagedResponse<GroupAccessReviewResponse>>>;

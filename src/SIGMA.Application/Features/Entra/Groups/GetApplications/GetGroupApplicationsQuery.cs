using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetApplications;

public sealed record GetGroupApplicationsQuery(string Id) : IQuery<Result<PagedResponse<GroupApplicationResponse>>>;

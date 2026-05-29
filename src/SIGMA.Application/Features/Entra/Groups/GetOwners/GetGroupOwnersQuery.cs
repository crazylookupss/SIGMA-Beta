using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetOwners;

public sealed record GetGroupOwnersQuery(string Id) : IQuery<Result<PagedResponse<GroupOwnerResponse>>>;

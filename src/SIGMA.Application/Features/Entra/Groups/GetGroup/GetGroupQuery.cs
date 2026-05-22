using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetGroup;

public sealed record GetGroupQuery(
    string Id,
    string? Select
) : IQuery<Result<GetGroupResponse>>;

using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Groups.GetMembers;

public sealed record GetGroupMembersQuery(string Id) : IQuery<Result<PagedResponse<GroupMemberResponse>>>;

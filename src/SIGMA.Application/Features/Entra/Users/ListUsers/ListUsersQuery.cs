using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Users.ListUsers;

public sealed record ListUsersQuery(
    string? Select,
    string? Filter,
    int? Top,
    int? Skip,
    bool? Count
) : IQuery<Result<PagedResponse<ListUsersResponse>>>;

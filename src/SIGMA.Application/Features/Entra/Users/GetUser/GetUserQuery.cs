using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Users.GetUser;

public sealed record GetUserQuery(
    string Id,
    string? Select
) : IQuery<Result<GetUserResponse>>;

using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Applications.GetApplication;

public sealed record GetApplicationQuery(
    string Id,
    string? Select
) : IQuery<Result<GetApplicationResponse>>;

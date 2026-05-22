using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipal;

public sealed record GetServicePrincipalQuery(
    string Id,
    string? Select
) : IQuery<Result<GetServicePrincipalResponse>>;

using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalSsoConfig;

public sealed record GetServicePrincipalSsoConfigQuery(
    string ServicePrincipalId
) : IQuery<Result<GetServicePrincipalSsoConfigResponse>>;

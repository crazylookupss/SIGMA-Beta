using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalProxyConfig;

public sealed record GetServicePrincipalProxyConfigQuery(string ServicePrincipalId)
    : IQuery<Result<GetServicePrincipalProxyConfigResponse>>;

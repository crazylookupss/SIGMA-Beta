using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalDashboard;

public sealed record GetServicePrincipalDashboardQuery : IQuery<Result<GetServicePrincipalDashboardResponse>>;

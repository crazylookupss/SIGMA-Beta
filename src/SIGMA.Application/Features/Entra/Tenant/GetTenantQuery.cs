using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Tenant;

public sealed record GetTenantQuery : IQuery<Result<GetTenantResponse>>;

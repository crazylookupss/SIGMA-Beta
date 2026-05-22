using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.ListServicePrincipals;

public sealed record ListServicePrincipalsQuery(
    string? Select,
    string? Filter,
    int? Top,
    int? Skip,
    bool? Count
) : IQuery<Result<PagedResponse<ListServicePrincipalsResponse>>>;

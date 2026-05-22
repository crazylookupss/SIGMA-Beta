using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Applications.ListApplications;

public sealed record ListApplicationsQuery(
    string? Select,
    string? Filter,
    int? Top,
    int? Skip,
    bool? Count
) : IQuery<Result<PagedResponse<ListApplicationsResponse>>>;

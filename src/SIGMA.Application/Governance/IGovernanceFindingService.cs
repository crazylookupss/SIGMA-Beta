using SIGMA.Application.Governance.Models;

namespace SIGMA.Application.Governance;

public interface IGovernanceFindingService
{
    Task<GovernanceFindingsResponse> GetFindingsAsync(bool forceRefresh, CancellationToken ct);
    Task<GovernanceFindingsResponse> GetFindingsByCategoryAsync(FindingCategory category, CancellationToken ct);
    Task<GovernanceFindingsResponse> GetFindingsBySeverityAsync(FindingSeverity severity, CancellationToken ct);
}

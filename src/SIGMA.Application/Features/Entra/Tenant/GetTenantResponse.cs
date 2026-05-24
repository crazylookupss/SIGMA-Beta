namespace SIGMA.Application.Features.Entra.Tenant;

public sealed record GetTenantResponse
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string PrimaryDomain { get; init; } = string.Empty;
    public string License { get; init; } = string.Empty;
    public int UsersCount { get; init; }
    public int GroupsCount { get; init; }
    public int ApplicationsCount { get; init; }
    public int EnterpriseApplicationsCount { get; init; }
    public int DevicesCount { get; init; }
}

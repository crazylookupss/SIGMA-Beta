namespace SIGMA.Domain.Entities;

public class EntraTenant
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PrimaryDomain { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public int UsersCount { get; set; }
    public int GroupsCount { get; set; }
    public int ApplicationsCount { get; set; }
    public int EnterpriseApplicationsCount { get; set; }
    public int DevicesCount { get; set; }
}

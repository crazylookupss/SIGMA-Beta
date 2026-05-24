namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalDashboard;

public sealed record GetServicePrincipalDashboardResponse
{
    public int TotalApplications { get; init; }
    public int ActiveCount { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
    public int TotalUsersCount { get; init; }
    public List<StatusStatDto> StatusStats { get; init; } = [];
    public List<SignInHistoryDto> SignInsHistory { get; init; } = [];
    public List<TopAppDto> TopApplications { get; init; } = [];
}

public sealed record StatusStatDto(string Status, int Count, double Percentage);

public sealed record SignInHistoryDto(string Date, int Count);

public sealed record TopAppDto
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? AppId { get; init; }
    public string SignInStatus { get; init; } = "Active";
    public int UsersCount { get; init; }
    public int SignInsCount { get; init; }
    public DateTimeOffset? LastSignIn { get; init; }
}

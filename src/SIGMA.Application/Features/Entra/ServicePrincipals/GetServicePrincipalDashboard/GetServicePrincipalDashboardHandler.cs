using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalDashboard;

internal sealed class GetServicePrincipalDashboardHandler(IGraphClientService graphClient)
    : IQueryHandler<GetServicePrincipalDashboardQuery, Result<GetServicePrincipalDashboardResponse>>
{
    public async Task<Result<GetServicePrincipalDashboardResponse>> Handle(
        GetServicePrincipalDashboardQuery query, CancellationToken cancellationToken)
    {
        // Fetch all enterprise-app service principals with enrichment from Microsoft Graph
        // The GraphClientService now applies the correct enterprise app filter
        // (tags/Any(x: x eq 'WindowsAzureActiveDirectoryIntegratedApp')) and follows pagination
        // to ensure ALL enterprise applications are returned, matching Entra Admin Center counts.
        var result = await graphClient.GetServicePrincipalsAsync(
            select: null, filter: null, top: null, skip: null, count: null,
            cancellationToken: cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var servicePrincipals = result.Value!.Data;

        // Aggregate counts and health statuses from enriched data
        var totalApps = servicePrincipals.Count;
        var activeCount = servicePrincipals.Count(sp => sp.SignInStatus == "Active");
        var warningCount = servicePrincipals.Count(sp => sp.SignInStatus == "Warning");
        var errorCount = servicePrincipals.Count(sp => sp.SignInStatus == "Error");
        var totalUsers = servicePrincipals.Sum(sp => sp.UsersCount);

        // Generate Donut Chart Segments
        var statusStats = new List<StatusStatDto>
        {
            new("Active", activeCount, totalApps > 0 ? Math.Round((double)activeCount / totalApps * 100, 1) : 0),
            new("Warning", warningCount, totalApps > 0 ? Math.Round((double)warningCount / totalApps * 100, 1) : 0),
            new("Error", errorCount, totalApps > 0 ? Math.Round((double)errorCount / totalApps * 100, 1) : 0)
        };

        // Sign-in history line chart data (requires Microsoft Entra ID P1/P2 license)
        var signInEntries = await graphClient.GetSignInHistoryAsync(30, cancellationToken);
        var signInsHistory = signInEntries
            .Where(s => s.CreatedDateTime.HasValue)
            .GroupBy(s => s.CreatedDateTime!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SignInHistoryDto(g.Key.ToString("yyyy-MM-dd"), g.Count()))
            .ToList();

        // Generate Top 5 Applications sorted by assigned users count
        var topApps = servicePrincipals
            .Select(sp => new TopAppDto
            {
                Id = sp.Id,
                DisplayName = sp.DisplayName ?? "Unknown Application",
                AppId = sp.AppId,
                SignInStatus = sp.SignInStatus,
                UsersCount = sp.UsersCount,
                SignInsCount = 0,
                LastSignIn = sp.LastSignIn
            })
            .OrderByDescending(t => t.UsersCount)
            .Take(5)
            .ToList();

        return Result.Success(new GetServicePrincipalDashboardResponse
        {
            TotalApplications = totalApps,
            ActiveCount = activeCount,
            WarningCount = warningCount,
            ErrorCount = errorCount,
            TotalUsersCount = totalUsers,
            StatusStats = statusStats,
            SignInsHistory = signInsHistory,
            TopApplications = topApps
        });
    }
}

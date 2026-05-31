using SIGMA.Application.Abstractions;
using SIGMA.Application.ProtocolAnalysis.Pipeline;

namespace SIGMA.Infrastructure.ProtocolAnalysis;

/// <summary>
/// Collects raw data from Microsoft Graph API for protocol analysis.
/// </summary>
internal sealed class GraphDataCollector : IDataCollector
{
    private readonly IGraphClientService _graphClient;

    public GraphDataCollector(IGraphClientService graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task<CollectedData> CollectAsync(string servicePrincipalId, CancellationToken ct)
    {
        var spResult = await _graphClient.GetServicePrincipalByIdAsync(servicePrincipalId, null, ct);
        if (spResult.IsFailure)
            throw new InvalidOperationException($"Failed to fetch service principal: {spResult.Error?.Description}");

        var sp = spResult.Value!;
        EntraApplicationDetails? appDetails = null;

        if (!string.IsNullOrEmpty(sp.AppId))
        {
            var appResult = await _graphClient.GetApplicationByAppIdAsync(sp.AppId, ct);
            if (appResult.IsSuccess)
            {
                appDetails = appResult.Value;
            }
        }

        return new CollectedData
        {
            ServicePrincipal = sp,
            ApplicationDetails = appDetails,
        };
    }
}

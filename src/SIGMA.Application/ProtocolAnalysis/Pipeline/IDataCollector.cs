using SIGMA.Domain.Entities;

namespace SIGMA.Application.ProtocolAnalysis.Pipeline;

/// <summary>
/// Raw data collected from external sources (Graph API).
/// </summary>
public sealed record CollectedData
{
    public EntraServicePrincipal ServicePrincipal { get; init; } = null!;
    public SIGMA.Application.Abstractions.EntraApplicationDetails? ApplicationDetails { get; init; }
}

/// <summary>
/// Stage 1: Collects raw data from Graph API.
/// </summary>
public interface IDataCollector
{
    Task<CollectedData> CollectAsync(string servicePrincipalId, CancellationToken ct);
}

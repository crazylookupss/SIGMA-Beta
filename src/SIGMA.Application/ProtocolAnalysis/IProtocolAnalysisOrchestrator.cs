using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.ProtocolAnalysis.Pipeline;

/// <summary>
/// Orchestrates the protocol analysis pipeline with 2-layer caching.
/// Layer 1: DetectionData (5 min TTL)
/// Layer 2: ProtocolAnalysisResult (2 min TTL)
/// </summary>
public interface IProtocolAnalysisOrchestrator
{
    Task<ProtocolAnalysisResult> AnalyzeAsync(string servicePrincipalId, CancellationToken ct);
}

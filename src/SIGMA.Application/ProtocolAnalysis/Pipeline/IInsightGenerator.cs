using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.ProtocolAnalysis.Pipeline;

/// <summary>
/// Stage 5: Generates actionable governance insights from analysis results.
/// </summary>
public interface IInsightGenerator
{
    List<GovernanceInsight> Generate(ProtocolAnalysisResult result, DetectionData data);
}

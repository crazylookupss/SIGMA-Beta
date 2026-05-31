using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.ProtocolAnalysis.Pipeline;

/// <summary>
/// Stage 4: Analyzes governance risks from protocol analysis results.
/// </summary>
public interface IGovernanceAnalyzer
{
    List<GovernanceInsight> Analyze(ProtocolAnalysisResult result, DetectionData data);
}

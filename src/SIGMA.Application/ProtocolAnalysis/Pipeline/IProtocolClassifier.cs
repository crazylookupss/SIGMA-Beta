using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.ProtocolAnalysis.Pipeline;

/// <summary>
/// Stage 3: Classifies protocol based on DetectionData evidence.
/// </summary>
public interface IProtocolClassifier
{
    Task<ProtocolAnalysisResult> ClassifyAsync(DetectionData data, CancellationToken cancellationToken = default);
}

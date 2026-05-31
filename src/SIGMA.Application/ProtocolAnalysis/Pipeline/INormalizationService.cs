using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.ProtocolAnalysis.Pipeline;

/// <summary>
/// Stage 2: Normalizes collected data into DetectionData for protocol analysis.
/// </summary>
public interface INormalizationService
{
    DetectionData Normalize(CollectedData data);
}

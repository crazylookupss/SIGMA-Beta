using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Abstractions;

public interface IProtocolAnalysisEngine
{
    Task<ProtocolAnalysisResult> AnalyzeAsync(DetectionData data, CancellationToken ct = default);
}

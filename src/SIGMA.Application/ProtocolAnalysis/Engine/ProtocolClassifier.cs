using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Application.ProtocolAnalysis.Pipeline;

namespace SIGMA.Application.ProtocolAnalysis.Engine;

/// <summary>
/// Classifies protocol based on DetectionData evidence using the existing engine.
/// </summary>
internal sealed class ProtocolClassifier : IProtocolClassifier
{
    private readonly IProtocolAnalysisEngine _engine;

    public ProtocolClassifier(IProtocolAnalysisEngine engine)
    {
        _engine = engine;
    }

    public async Task<ProtocolAnalysisResult> ClassifyAsync(DetectionData data, CancellationToken cancellationToken = default)
    {
        return await _engine.AnalyzeAsync(data, cancellationToken);
    }
}

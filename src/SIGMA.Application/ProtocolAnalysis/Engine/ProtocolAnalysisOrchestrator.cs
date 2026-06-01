using SIGMA.Application.Caching;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Application.ProtocolAnalysis.Pipeline;

namespace SIGMA.Application.ProtocolAnalysis.Engine;

/// <summary>
/// Orchestrates the protocol analysis pipeline with 2-layer caching.
/// </summary>
internal sealed class ProtocolAnalysisOrchestrator : IProtocolAnalysisOrchestrator
{
    private readonly ICacheProvider _cache;
    private readonly IDataCollector _collector;
    private readonly INormalizationService _normalizer;
    private readonly IProtocolClassifier _classifier;
    private readonly IGovernanceAnalyzer _governanceAnalyzer;
    private readonly IInsightGenerator _insightGenerator;

    private static readonly TimeSpan DetectionDataCacheTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AnalysisResultCacheTtl = TimeSpan.FromMinutes(2);

    public ProtocolAnalysisOrchestrator(
        ICacheProvider cache,
        IDataCollector collector,
        INormalizationService normalizer,
        IProtocolClassifier classifier,
        IGovernanceAnalyzer governanceAnalyzer,
        IInsightGenerator insightGenerator)
    {
        _cache = cache;
        _collector = collector;
        _normalizer = normalizer;
        _classifier = classifier;
        _governanceAnalyzer = governanceAnalyzer;
        _insightGenerator = insightGenerator;
    }

    public async Task<ProtocolAnalysisResult> AnalyzeAsync(string servicePrincipalId, CancellationToken ct)
    {
        // Layer 2: Check ProtocolAnalysisResult cache
        var resultCacheKey = $"protocol_analysis_{servicePrincipalId}";
        var cachedResult = await _cache.GetAsync<ProtocolAnalysisResult>(resultCacheKey, ct);
        if (cachedResult is not null)
            return cachedResult;

        // Layer 1: Check DetectionData cache
        var dataCacheKey = $"detection_data_{servicePrincipalId}";
        var detectionData = await _cache.GetAsync<DetectionData>(dataCacheKey, ct);

        if (detectionData is null)
        {
            // Pipeline: Collect → Normalize
            var collected = await _collector.CollectAsync(servicePrincipalId, ct);
            detectionData = _normalizer.Normalize(collected);

            // Cache DetectionData (Layer 1)
            await _cache.SetAsync(dataCacheKey, detectionData, DetectionDataCacheTtl, ct);
        }

        // Pipeline: Classify → Analyze → Generate
        var result = await _classifier.ClassifyAsync(detectionData!, ct);
        var governanceInsights = _governanceAnalyzer.Analyze(result, detectionData!);
        var generatedInsights = _insightGenerator.Generate(result, detectionData!);

        // Merge all insights
        var allInsights = governanceInsights.Concat(generatedInsights).ToList();

        var finalResult = result with { GovernanceInsights = allInsights };

        // Cache ProtocolAnalysisResult (Layer 2)
        await _cache.SetAsync(resultCacheKey, finalResult, AnalysisResultCacheTtl, ct);

        return finalResult;
    }
}

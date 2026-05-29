using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Engine;

internal sealed class ProtocolAnalysisEngine : IProtocolAnalysisEngine
{
    private readonly IEnumerable<IProtocolDetector> _detectors;

    public ProtocolAnalysisEngine(IEnumerable<IProtocolDetector> detectors)
    {
        _detectors = detectors;
    }

    public Task<ProtocolAnalysisResult> AnalyzeAsync(DetectionData data, CancellationToken ct = default)
    {
        var detections = new List<ProtocolDetection>();
        var allEvidence = new List<ProtocolEvidence>();

        foreach (var detector in _detectors)
        {
            var detection = detector.DetectAsync(data, ct).GetAwaiter().GetResult();
            if (detection.IsDetected)
            {
                detections.Add(detection);
                allEvidence.AddRange(detection.Evidence);
            }
        }

        detections = [.. detections.OrderByDescending(d => d.Score)];

        var primaryProtocol = detections.Count > 0
            ? detections[0].Protocol
            : AuthenticationProtocol.Unknown;

        var insights = BuildGovernanceInsights(detections, data);

        return Task.FromResult(new ProtocolAnalysisResult
        {
            PrimaryProtocol = primaryProtocol,
            DetectedProtocols = detections,
            AllEvidence = allEvidence,
            GovernanceInsights = insights,
            AnalysisTimestamp = DateTimeOffset.UtcNow,
            ServicePrincipalId = data.ServicePrincipalId,
            ServicePrincipalDisplayName = data.DisplayName,
        });
    }

    private static List<GovernanceInsight> BuildGovernanceInsights(
        List<ProtocolDetection> detections, DetectionData data)
    {
        var insights = new List<GovernanceInsight>();

        if (detections.Count == 0)
        {
            insights.Add(new GovernanceInsight
            {
                Severity = "Warning",
                Category = "NoProtocol",
                Message = "No supported SSO protocol could be detected. This application may be using a custom authentication method or client credentials flow."
            });

            if (string.IsNullOrEmpty(data.PreferredSingleSignOnMode))
            {
                insights.Add(new GovernanceInsight
                {
                    Severity = "Info",
                    Category = "MissingConfiguration",
                    Message = "The 'preferredSingleSignOnMode' field is not set on the service principal. Consider configuring SSO through the Entra admin center."
                });
            }
        }
        else if (detections.Count > 1)
        {
            var primary = detections[0].Protocol;
            var others = string.Join(", ", detections.Skip(1).Select(d => d.Protocol));
            insights.Add(new GovernanceInsight
            {
                Severity = "Info",
                Category = "MultipleProtocols",
                Message = $"Multiple authentication protocols detected. Primary: {primary}. Also detected: {others}."
            });

            if (detections.Any(d => d.Confidence == ProtocolConfidence.High) &&
                detections.Count(d => d.Confidence == ProtocolConfidence.High) > 1)
            {
                insights.Add(new GovernanceInsight
                {
                    Severity = "Warning",
                    Category = "AmbiguousProtocol",
                    Message = "Multiple protocols have high confidence scores. Review the evidence list to determine the intended SSO method."
                });
            }
        }

        return insights;
    }
}

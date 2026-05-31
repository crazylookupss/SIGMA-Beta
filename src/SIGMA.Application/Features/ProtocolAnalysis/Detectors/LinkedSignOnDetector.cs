using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class LinkedSignOnDetector : IProtocolDetector
{
    private const int MaxPossibleScore = 40;

    public AuthenticationProtocol Protocol => AuthenticationProtocol.LinkedSignOn;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Critical: PreferredSingleSignOnMode == "linked"
        if (string.Equals(data.PreferredSingleSignOnMode, "linked", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "PreferredSingleSignOnMode",
                Value = data.PreferredSingleSignOnMode,
                Weight = 30,
                Description = "Preferred single sign-on mode is explicitly set to Linked Sign-On",
                Category = "ExplicitConfiguration"
            });
            score += 30;
        }

        // Medium: Has linked configuration indicators
        if (score > 0 && data.RedirectUris.Count == 0 &&
            string.IsNullOrEmpty(data.SamlMetadataUrl) &&
            data.KeyCredentials.Count == 0 &&
            data.ExposedScopeValues.Count == 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Analysis",
                Field = "Inferred",
                Weight = 10,
                Description = "No native protocol configuration detected — consistent with linked sign-on behavior",
                Category = "Inferred"
            });
            score += 10;
        }

        // Confidence based on normalized score
        var normalizedScore = MaxPossibleScore > 0 ? (double)score / MaxPossibleScore * 100 : 0;
        var confidence = normalizedScore switch
        {
            >= 30 => ProtocolConfidence.High,
            >= 15 => ProtocolConfidence.Medium,
            > 0 => ProtocolConfidence.Low,
            _ => ProtocolConfidence.Low
        };

        return Task.FromResult(new ProtocolDetection
        {
            Protocol = AuthenticationProtocol.LinkedSignOn,
            Confidence = confidence,
            Score = Math.Max(score, 0),
            MaxPossibleScore = MaxPossibleScore,
            Evidence = evidence
        });
    }
}

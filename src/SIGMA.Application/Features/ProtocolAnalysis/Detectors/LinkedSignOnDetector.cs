using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class LinkedSignOnDetector : IProtocolDetector
{
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
        // Linked sign-on apps typically have minimal Graph configuration
        // (no redirect URIs, no SAML metadata, no certs) but exist as enterprise apps.
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

        var confidence = score switch
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
            Score = score,
            Evidence = evidence
        });
    }
}

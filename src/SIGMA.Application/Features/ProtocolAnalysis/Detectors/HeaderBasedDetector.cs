using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class HeaderBasedDetector : IProtocolDetector
{
    public AuthenticationProtocol Protocol => AuthenticationProtocol.HeaderBased;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Critical: PreferredSingleSignOnMode == "headerBased"
        if (string.Equals(data.PreferredSingleSignOnMode, "headerBased", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(data.PreferredSingleSignOnMode, "headerbased", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "PreferredSingleSignOnMode",
                Value = data.PreferredSingleSignOnMode,
                Weight = 30,
                Description = "Preferred single sign-on mode is explicitly set to Header-Based",
                Category = "ExplicitConfiguration"
            });
            score += 30;
        }

        // Medium: Has notification emails (commonly configured for header-based SSO)
        if (data.NotificationEmailAddresses.Count > 0 && score > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "NotificationEmailAddresses",
                Weight = 10,
                Description = "Notification emails configured (common for header-based SSO)",
                Category = "GeneralConfiguration"
            });
            score += 10;
        }

        // Low: Has custom sign-in URL pattern (header-based apps often have specific URLs)
        // We can't directly access customSignInUrl from current Graph data, but the
        // presence of certain tags may indicate header-based configuration.

        var confidence = score switch
        {
            >= 30 => ProtocolConfidence.High,
            >= 15 => ProtocolConfidence.Medium,
            > 0 => ProtocolConfidence.Low,
            _ => ProtocolConfidence.Low
        };

        return Task.FromResult(new ProtocolDetection
        {
            Protocol = AuthenticationProtocol.HeaderBased,
            Confidence = confidence,
            Score = score,
            Evidence = evidence
        });
    }
}

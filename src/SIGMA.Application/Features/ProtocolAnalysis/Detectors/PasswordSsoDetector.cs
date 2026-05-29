using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class PasswordSsoDetector : IProtocolDetector
{
    public AuthenticationProtocol Protocol => AuthenticationProtocol.PasswordBased;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Critical: PreferredSingleSignOnMode == "password"
        if (string.Equals(data.PreferredSingleSignOnMode, "password", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "PreferredSingleSignOnMode",
                Value = data.PreferredSingleSignOnMode,
                Weight = 30,
                Description = "Preferred single sign-on mode is explicitly set to Password-Based SSO",
                Category = "ExplicitConfiguration"
            });
            score += 30;
        }

        // Low: Notification emails (commonly configured for password SSO)
        if (data.NotificationEmailAddresses.Count > 0 && score > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "NotificationEmailAddresses",
                Weight = 5,
                Description = "Notification emails configured for password SSO notifications",
                Category = "GeneralConfiguration"
            });
            score += 5;
        }

        // Low: Has home page URL (password SSO apps typically have a sign-in page)
        if (!string.IsNullOrEmpty(data.HomePageUrl) && score > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "HomePageUrl",
                Value = data.HomePageUrl,
                Weight = 5,
                Description = "Home page URL configured (password SSO redirects users here)",
                Category = "GeneralConfiguration"
            });
            score += 5;
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
            Protocol = AuthenticationProtocol.PasswordBased,
            Confidence = confidence,
            Score = score,
            Evidence = evidence
        });
    }
}

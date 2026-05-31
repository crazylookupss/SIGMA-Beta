using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class HeaderBasedDetector : IProtocolDetector
{
    private const int MaxPossibleScore = 90;

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

        // NEW (Critical): CustomSingleSignOnUrl present — THE primary signal for header-based
        if (!string.IsNullOrEmpty(data.CustomSingleSignOnUrl))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "CustomSingleSignOnUrl",
                Value = data.CustomSingleSignOnUrl,
                Weight = 25,
                Description = "Custom sign-on URL configured for header-based SSO",
                Category = "HeaderConfiguration"
            });
            score += 25;
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

        // NEW: No SAML indicators (negative evidence)
        var hasSamlIndicators = !string.IsNullOrEmpty(data.SamlMetadataUrl) ||
            data.KeyCredentials.Any(k => k.Type == "AsymmetricX509Cert") ||
            !string.IsNullOrEmpty(data.TokenEncryptionKeyId);
        if (!hasSamlIndicators && score > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "SamlMetadataUrl",
                Weight = 10,
                Description = "No SAML configuration indicators found (supports header-based classification)",
                Category = "NegativeEvidence"
            });
            score += 10;
        }

        // NEW: No OIDC indicators (negative evidence)
        var hasOidcIndicators = data.EnableIdTokenIssuance == true ||
            data.RedirectUris.Any(u => u.StartsWith("http")) ||
            data.PublicClientRedirectUris.Count > 0;
        if (!hasOidcIndicators && score > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "EnableIdTokenIssuance",
                Weight = 10,
                Description = "No OIDC configuration indicators found (supports header-based classification)",
                Category = "NegativeEvidence"
            });
            score += 10;
        }

        // NEW: AppRoleAssignmentRequired (common for header-based apps)
        if (data.ServicePrincipalTags.Any(t => t.Contains("WindowsAzureActiveDirectoryIntegratedApp", StringComparison.OrdinalIgnoreCase)) && score > 0)
        {
            // This is actually a weak signal but helps in combination
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
            Protocol = AuthenticationProtocol.HeaderBased,
            Confidence = confidence,
            Score = Math.Max(score, 0),
            MaxPossibleScore = MaxPossibleScore,
            Evidence = evidence
        });
    }
}

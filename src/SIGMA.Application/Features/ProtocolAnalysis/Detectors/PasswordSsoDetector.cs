using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class PasswordSsoDetector : IProtocolDetector
{
    private const int MaxPossibleScore = 85;

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

        // NEW (Strong): CustomSingleSignOnUrl present — password vault URL
        if (!string.IsNullOrEmpty(data.CustomSingleSignOnUrl))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "CustomSingleSignOnUrl",
                Value = data.CustomSingleSignOnUrl,
                Weight = 20,
                Description = "Custom sign-on URL configured for password vault SSO",
                Category = "PasswordConfiguration"
            });
            score += 20;
        }

        // NEW (Strong): PasswordCredentials present — password vault has credentials
        if (data.PasswordCredentials.Count > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "PasswordCredentials",
                Weight = 15,
                Description = $"{data.PasswordCredentials.Count} password credential(s) configured in password vault",
                Category = "PasswordConfiguration"
            });
            score += 15;
        }

        // Medium: Notification emails (commonly configured for password SSO)
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

        // NEW: No SAML/OIDC indicators (negative evidence)
        var hasSamlIndicators = !string.IsNullOrEmpty(data.SamlMetadataUrl) ||
            data.KeyCredentials.Any(k => k.Type == "AsymmetricX509Cert");
        var hasOidcIndicators = data.EnableIdTokenIssuance == true ||
            data.RedirectUris.Any(u => u.StartsWith("http"));
        if (!hasSamlIndicators && !hasOidcIndicators && score > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "SamlMetadataUrl",
                Weight = 10,
                Description = "No SAML or OIDC configuration indicators found (supports password-based classification)",
                Category = "NegativeEvidence"
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
            Protocol = AuthenticationProtocol.PasswordBased,
            Confidence = confidence,
            Score = Math.Max(score, 0),
            MaxPossibleScore = MaxPossibleScore,
            Evidence = evidence
        });
    }
}

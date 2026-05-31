using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class WsFedProtocolDetector : IProtocolDetector
{
    private const int MaxPossibleScore = 70;

    public AuthenticationProtocol Protocol => AuthenticationProtocol.WsFed;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Critical: PreferredSingleSignOnMode == "wsFed"
        if (string.Equals(data.PreferredSingleSignOnMode, "wsFed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(data.PreferredSingleSignOnMode, "wsfed", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "PreferredSingleSignOnMode",
                Value = data.PreferredSingleSignOnMode,
                Weight = 30,
                Description = "Preferred single sign-on mode is explicitly set to WS-Federation",
                Category = "ExplicitConfiguration"
            });
            score += 30;
        }

        // Strong: WS-Federation passive sign-in endpoint pattern in identifier URIs
        if (data.IdentifierUris.Any(u =>
            u.Contains("adfs", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("wsfed", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("federation", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "IdentifierUris",
                Value = string.Join("; ", data.IdentifierUris),
                Weight = 15,
                Description = "Identifier URIs reference ADFS or WS-Federation endpoint",
                Category = "WsFedConfiguration"
            });
            score += 15;
        }

        // Medium: WsFed-specific tag patterns
        var hasWsFedTag = data.ApplicationTags.Any(t =>
            t.Contains("WindowsAzureActiveDirectoryCustomSingleSignOnApplication", StringComparison.OrdinalIgnoreCase) &&
            !data.ApplicationTags.Any(t2 =>
                t2.Contains("GalleryApplicationNonPrimaryV1", StringComparison.OrdinalIgnoreCase)));
        if (hasWsFedTag)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "Tags",
                Weight = 10,
                Description = "Application has custom SSO tag without gallery tag — may be WS-Fed",
                Category = "WsFedConfiguration"
            });
            score += 10;
        }

        // Medium: Has federation metadata URL (constructed by Entra)
        // The federation metadata URL is on the tenant level, not per-app, so we can't
        // directly detect it here. But we can check for WS-Federation passive sign-on
        // endpoint URL patterns in the configuration.

        // Medium: IdentifierUris with https:// and no SAML/OIDC signals
        if (data.IdentifierUris.Any(u =>
            u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrEmpty(data.SamlMetadataUrl) &&
            data.EnableIdTokenIssuance != true)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "IdentifierUris",
                Value = string.Join("; ", data.IdentifierUris),
                Weight = 10,
                Description = "Has HTTPS identifier URIs without SAML metadata or OIDC implicit grant — potential WS-Fed",
                Category = "WsFedConfiguration"
            });
            score += 10;
        }

        // Low: HomePageUrl exists (WS-Fed apps typically have a homepage URL)
        if (!string.IsNullOrEmpty(data.HomePageUrl) && score > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "HomePageUrl",
                Value = data.HomePageUrl,
                Weight = 5,
                Description = "Home page URL configured (common for WS-Federation apps)",
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
            Protocol = AuthenticationProtocol.WsFed,
            Confidence = confidence,
            Score = Math.Max(score, 0),
            MaxPossibleScore = MaxPossibleScore,
            Evidence = evidence
        });
    }
}

using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class SamlProtocolDetector : IProtocolDetector
{
    public AuthenticationProtocol Protocol => AuthenticationProtocol.Saml;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Critical: PreferredSingleSignOnMode == "saml"
        if (string.Equals(data.PreferredSingleSignOnMode, "saml", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "PreferredSingleSignOnMode",
                Value = data.PreferredSingleSignOnMode,
                Weight = 30,
                Description = "Preferred single sign-on mode is explicitly set to SAML",
                Category = "ExplicitConfiguration"
            });
            score += 30;
        }

        // Strong: SAML metadata URL exists
        if (!string.IsNullOrEmpty(data.SamlMetadataUrl))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "SamlMetadataUrl",
                Value = data.SamlMetadataUrl,
                Weight = 25,
                Description = "SAML metadata URL is configured on the app registration",
                Category = "SamlConfiguration"
            });
            score += 25;
        }

        // Strong: Custom SAML SSO tag
        if (data.ApplicationTags.Any(t =>
            t.Contains("WindowsAzureActiveDirectoryCustomSingleSignOnApplication", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "Tags",
                Value = "WindowsAzureActiveDirectoryCustomSingleSignOnApplication",
                Weight = 20,
                Description = "Application is tagged as a custom SAML SSO application",
                Category = "SamlConfiguration"
            });
            score += 20;
        }
        else if (data.ServicePrincipalTags.Any(t =>
            t.Contains("WindowsAzureActiveDirectoryCustomSingleSignOnApplication", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "Tags",
                Value = "WindowsAzureActiveDirectoryCustomSingleSignOnApplication",
                Weight = 20,
                Description = "Service principal is tagged as a custom SAML SSO application",
                Category = "SamlConfiguration"
            });
            score += 20;
        }

        // Medium: Gallery SAML tag
        var hasGalleryTag = (data.ApplicationTags.Any(t =>
            t.Contains("WindowsAzureActiveDirectoryGalleryApplicationNonPrimaryV1", StringComparison.OrdinalIgnoreCase))
            || data.ServicePrincipalTags.Any(t =>
                t.Contains("WindowsAzureActiveDirectoryGalleryApplicationNonPrimaryV1", StringComparison.OrdinalIgnoreCase)));
        if (hasGalleryTag)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Tags",
                Field = "Tags",
                Value = "WindowsAzureActiveDirectoryGalleryApplicationNonPrimaryV1",
                Weight = 10,
                Description = "Application is tagged as a gallery SAML application",
                Category = "SamlConfiguration"
            });
            score += 10;
        }

        // Medium: IdentifierUris with SAML entity pattern
        if (data.IdentifierUris.Any(u =>
            u.StartsWith("urn:", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("spn:", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "IdentifierUris",
                Value = string.Join("; ", data.IdentifierUris),
                Weight = 10,
                Description = "Identifier URIs follow SAML entity pattern (urn:/spn:)",
                Category = "SamlConfiguration"
            });
            score += 10;
        }

        // Medium: X509 certificate present (Sign/Verify usage)
        var hasSamlCert = data.KeyCredentials.Any(k =>
            (string.Equals(k.Type, "AsymmetricX509Cert", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(k.Usage, "Verify", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(k.Usage, "Sign", StringComparison.OrdinalIgnoreCase)));
        if (hasSamlCert)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "KeyCredentials",
                Weight = 15,
                Description = "Application has X.509 certificates for SAML signing/verification",
                Category = "SamlConfiguration"
            });
            score += 15;
        }

        // Medium: Token encryption key
        if (!string.IsNullOrEmpty(data.TokenEncryptionKeyId))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "TokenEncryptionKeyId",
                Value = data.TokenEncryptionKeyId,
                Weight = 10,
                Description = "Token encryption key is configured (SAML encrypted assertion feature)",
                Category = "SamlConfiguration"
            });
            score += 10;
        }

        // Low: ACS-style redirect URIs
        if (data.RedirectUris.Any(u =>
            u.Contains("/saml", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("/acs", StringComparison.OrdinalIgnoreCase) ||
            u.Contains("Saml", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RedirectUris",
                Weight = 5,
                Description = "Redirect URIs contain SAML ACS pattern",
                Category = "SamlConfiguration"
            });
            score += 5;
        }

        var confidence = score switch
        {
            >= 35 => ProtocolConfidence.High,
            >= 20 => ProtocolConfidence.Medium,
            > 0 => ProtocolConfidence.Low,
            _ => ProtocolConfidence.Low
        };

        return Task.FromResult(new ProtocolDetection
        {
            Protocol = AuthenticationProtocol.Saml,
            Confidence = confidence,
            Score = score,
            Evidence = evidence
        });
    }
}

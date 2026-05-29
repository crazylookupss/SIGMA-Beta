using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class ScimProvisioningDetector : IProtocolDetector
{
    public AuthenticationProtocol Protocol => AuthenticationProtocol.ScimProvisioning;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Strong: SCIM-related tag or display name
        var hasScimIndicator = data.ApplicationTags.Any(t =>
            t.Contains("scim", StringComparison.OrdinalIgnoreCase)) ||
            data.ServicePrincipalTags.Any(t =>
                t.Contains("scim", StringComparison.OrdinalIgnoreCase)) ||
            (data.DisplayName?.Contains("scim", StringComparison.OrdinalIgnoreCase) == true) ||
            (data.DisplayName?.Contains("provision", StringComparison.OrdinalIgnoreCase) == true);
        if (hasScimIndicator)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Tags",
                Field = "Tags/DisplayName",
                Value = data.DisplayName,
                Weight = 20,
                Description = "Application name or tags indicate SCIM provisioning",
                Category = "ScimConfiguration"
            });
            score += 20;
        }

        // Medium: Has notification email addresses (provisioning notifications)
        if (data.NotificationEmailAddresses.Count > 0 && (hasScimIndicator || score > 0))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "NotificationEmailAddresses",
                Weight = 10,
                Description = "Notification emails configured — used for provisioning alerts",
                Category = "ProvisioningConfiguration"
            });
            score += 10;
        }

        // Medium: Application has no redirect URIs or OIDC/SAML config (pure provisioning app)
        if (data.RedirectUris.Count == 0 &&
            string.IsNullOrEmpty(data.SamlMetadataUrl) &&
            data.EnableIdTokenIssuance != true &&
            data.RequiredResourceAccess.Count > 0 && hasScimIndicator)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Analysis",
                Field = "Inferred",
                Weight = 10,
                Description = "No SSO protocol config with API permissions — SCIM provisioning pattern",
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
            Protocol = AuthenticationProtocol.ScimProvisioning,
            Confidence = confidence,
            Score = score,
            Evidence = evidence
        });
    }
}

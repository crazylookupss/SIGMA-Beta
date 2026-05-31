using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class OidcProtocolDetector : IProtocolDetector
{
    private const int MaxPossibleScore = 135;

    public AuthenticationProtocol Protocol => AuthenticationProtocol.OpenIdConnect;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Critical: PreferredSingleSignOnMode == "oidc"
        if (string.Equals(data.PreferredSingleSignOnMode, "oidc", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "ServicePrincipal",
                Field = "PreferredSingleSignOnMode",
                Value = data.PreferredSingleSignOnMode,
                Weight = 30,
                Description = "Preferred single sign-on mode is explicitly set to OpenID Connect",
                Category = "ExplicitConfiguration"
            });
            score += 30;
        }

        // Strong: ID token issuance enabled (implicit grant)
        if (data.EnableIdTokenIssuance == true)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "EnableIdTokenIssuance",
                Value = "true",
                Weight = 25,
                Description = "Implicit grant flow enables ID token issuance (OIDC-specific)",
                Category = "OidcConfiguration"
            });
            score += 25;
        }

        // Medium: Access token issuance enabled
        if (data.EnableAccessTokenIssuance == true)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "EnableAccessTokenIssuance",
                Value = "true",
                Weight = 10,
                Description = "Implicit grant flow enables access token issuance",
                Category = "OidcConfiguration"
            });
            score += 10;
        }

        // Medium: HTTP/HTTPS redirect URIs
        var httpRedirects = data.RedirectUris.Where(u =>
            u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)).ToList();
        if (httpRedirects.Count > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RedirectUris",
                Value = string.Join("; ", httpRedirects),
                Weight = 15,
                Description = $"Application has {httpRedirects.Count} HTTP(S) redirect URI(s) — typical for OIDC web apps",
                Category = "RedirectUri"
            });
            score += 15;
        }

        // Medium: Public client redirects (mobile/desktop)
        if (data.PublicClientRedirectUris.Count > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "PublicClient.RedirectUris",
                Weight = 10,
                Description = "Public client redirect URIs configured (mobile/desktop OIDC)",
                Category = "RedirectUri"
            });
            score += 10;
        }

        // Medium: IdentifierUris with https:// or api:// pattern
        if (data.IdentifierUris.Any(u =>
            u.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            u.StartsWith("api://", StringComparison.OrdinalIgnoreCase)))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "IdentifierUris",
                Value = string.Join("; ", data.IdentifierUris),
                Weight = 10,
                Description = "Identifier URIs use HTTP/API scheme — typical for OIDC",
                Category = "Identifier"
            });
            score += 10;
        }

        // Medium: RequestedAccessTokenVersion == 2 (Microsoft Identity Platform v2)
        if (data.RequestedAccessTokenVersion == 2)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RequestedAccessTokenVersion",
                Value = "2",
                Weight = 10,
                Description = "Access token version 2 indicates Microsoft Identity Platform (OIDC-aware)",
                Category = "ApiConfiguration"
            });
            score += 10;
        }

        // Low: IsFallbackPublicClient
        if (data.IsFallbackPublicClient == true)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "IsFallbackPublicClient",
                Value = "true",
                Weight = 5,
                Description = "Application is configured as fallback public client",
                Category = "ClientConfiguration"
            });
            score += 5;
        }

        // NEW: GroupMembershipClaims present (OIDC token configuration)
        if (!string.IsNullOrEmpty(data.GroupMembershipClaims))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "GroupMembershipClaims",
                Value = data.GroupMembershipClaims,
                Weight = 5,
                Description = "Group membership claims configured in token settings (OIDC-specific)",
                Category = "OidcConfiguration"
            });
            score += 5;
        }

        // NEW: OptionalClaims present (OIDC custom claims)
        if (data.OptionalClaims is not null)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "OptionalClaims",
                Weight = 5,
                Description = "Optional claims configured in token settings (OIDC-specific)",
                Category = "OidcConfiguration"
            });
            score += 5;
        }

        // NEW: PreAuthorizedApplications present (OIDC pre-auth)
        if (data.PreAuthorizedApplicationsCount > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "PreAuthorizedApplications",
                Weight = 5,
                Description = $"{data.PreAuthorizedApplicationsCount} pre-authorized application(s) configured (OIDC pattern)",
                Category = "OidcConfiguration"
            });
            score += 5;
        }

        // NEW: LogoutUrl present (post-logout redirect)
        if (!string.IsNullOrEmpty(data.LogoutUrl))
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "LogoutUrl",
                Value = data.LogoutUrl,
                Weight = 5,
                Description = "Logout URL configured (post-logout redirect, typical for OIDC)",
                Category = "OidcConfiguration"
            });
            score += 5;
        }

        // Confidence based on normalized score
        var normalizedScore = MaxPossibleScore > 0 ? (double)score / MaxPossibleScore * 100 : 0;
        var confidence = normalizedScore switch
        {
            >= 35 => ProtocolConfidence.High,
            >= 20 => ProtocolConfidence.Medium,
            > 0 => ProtocolConfidence.Low,
            _ => ProtocolConfidence.Low
        };

        return Task.FromResult(new ProtocolDetection
        {
            Protocol = AuthenticationProtocol.OpenIdConnect,
            Confidence = confidence,
            Score = Math.Max(score, 0),
            MaxPossibleScore = MaxPossibleScore,
            Evidence = evidence
        });
    }
}

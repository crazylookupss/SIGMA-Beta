using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;

namespace SIGMA.Application.Features.ProtocolAnalysis.Detectors;

internal sealed class OAuth2ProtocolDetector : IProtocolDetector
{
    private const int MaxPossibleScore = 85;

    public AuthenticationProtocol Protocol => AuthenticationProtocol.OAuth2;

    public Task<ProtocolDetection> DetectAsync(DetectionData data, CancellationToken ct = default)
    {
        var evidence = new List<ProtocolEvidence>();
        var score = 0;

        // Strong: Application exposes OAuth2 scopes
        if (data.ExposedScopeValues.Count > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "Api.Oauth2PermissionScopes",
                Value = string.Join("; ", data.ExposedScopeValues),
                Weight = 20,
                Description = $"Application exposes {data.ExposedScopeValues.Count} OAuth 2.0 permission scope(s)",
                Category = "OAuth2Configuration"
            });
            score += 20;
        }

        // Strong: Custom scheme redirect URIs (non-HTTP)
        var nonHttpRedirects = data.RedirectUris.Where(u =>
            !u.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !u.StartsWith("https://", StringComparison.OrdinalIgnoreCase)).ToList();
        if (nonHttpRedirects.Count > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RedirectUris",
                Value = string.Join("; ", nonHttpRedirects),
                Weight = 15,
                Description = $"Application has {nonHttpRedirects.Count} custom scheme redirect URI(s) — typical for native OAuth 2.0 apps",
                Category = "RedirectUri"
            });
            score += 15;
        }

        // Medium: Required resource access (delegated permissions)
        var hasDelegated = data.RequiredResourceAccess
            .SelectMany(r => r.ResourceAccess)
            .Any(a => string.Equals(a.Type, "Scope", StringComparison.OrdinalIgnoreCase));
        if (hasDelegated)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RequiredResourceAccess",
                Weight = 10,
                Description = "Application has delegated permission requests — OAuth 2.0 auth code flow",
                Category = "Permissions"
            });
            score += 10;
        }

        // Medium: Has application permissions
        var hasAppPermissions = data.RequiredResourceAccess
            .SelectMany(r => r.ResourceAccess)
            .Any(a => string.Equals(a.Type, "Role", StringComparison.OrdinalIgnoreCase));
        if (hasAppPermissions)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RequiredResourceAccess",
                Weight = 10,
                Description = "Application has application permission requests — client credentials flow",
                Category = "Permissions"
            });
            score += 10;
        }

        // Medium: RequestedAccessTokenVersion == 1 (v1.0 = OAuth2, not OIDC-aware)
        if (data.RequestedAccessTokenVersion == 1)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RequestedAccessTokenVersion",
                Value = "1",
                Weight = 10,
                Description = "Access token version 1 indicates OAuth 2.0 (non-OIDC) token format",
                Category = "ApiConfiguration"
            });
            score += 10;
        }

        // Medium: AcceptMappedClaims — used in OAuth2 client cred scenarios
        if (data.AcceptMappedClaims == true)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "AcceptMappedClaims",
                Value = "true",
                Weight = 10,
                Description = "Accept mapped claims enabled — used in OAuth 2.0 token configuration",
                Category = "ApiConfiguration"
            });
            score += 10;
        }

        // FIX: Only count generic redirect URIs if no specific redirect type was already counted
        // This prevents double-counting with non-HTTP or HTTP(S) checks above
        if (data.RedirectUris.Count > 0 && nonHttpRedirects.Count == 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "RedirectUris",
                Weight = 5,
                Description = "Has web redirect URIs configured (generic OAuth2 pattern)",
                Category = "RedirectUri"
            });
            score += 5;
        }

        // NEW: KnownClientApplications present (backend client linking)
        if (data.KnownClientApplicationsCount > 0)
        {
            evidence.Add(new ProtocolEvidence
            {
                Source = "Application",
                Field = "KnownClientApplications",
                Weight = 5,
                Description = $"{data.KnownClientApplicationsCount} known client application(s) linked (OAuth2 backend pattern)",
                Category = "OAuth2Configuration"
            });
            score += 5;
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
            Protocol = AuthenticationProtocol.OAuth2,
            Confidence = confidence,
            Score = Math.Max(score, 0),
            MaxPossibleScore = MaxPossibleScore,
            Evidence = evidence
        });
    }
}

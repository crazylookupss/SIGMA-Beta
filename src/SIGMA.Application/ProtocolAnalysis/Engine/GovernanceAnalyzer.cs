using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Application.ProtocolAnalysis.Pipeline;

namespace SIGMA.Application.ProtocolAnalysis.Engine;

/// <summary>
/// Analyzes governance risks from protocol analysis results.
/// </summary>
internal sealed class GovernanceAnalyzer : IGovernanceAnalyzer
{
    public List<GovernanceInsight> Analyze(ProtocolAnalysisResult result, DetectionData data)
    {
        var insights = new List<GovernanceInsight>();

        // Certificate expiry analysis
        var now = DateTimeOffset.UtcNow;
        foreach (var credential in data.KeyCredentials)
        {
            if (credential.EndDateTime.HasValue)
            {
                var daysUntilExpiry = (credential.EndDateTime.Value - now).TotalDays;

                if (daysUntilExpiry < 0)
                {
                    insights.Add(new GovernanceInsight
                    {
                        Severity = GovernanceSeverity.Critical,
                        Category = GovernanceCategory.Certificate,
                        Message = $"Certificate '{credential.DisplayName}' is expired since {credential.EndDateTime.Value:yyyy-MM-dd}."
                    });
                }
                else if (daysUntilExpiry <= 30)
                {
                    insights.Add(new GovernanceInsight
                    {
                        Severity = GovernanceSeverity.Warning,
                        Category = GovernanceCategory.Certificate,
                        Message = $"Certificate '{credential.DisplayName}' expires in {daysUntilExpiry:F0} days ({credential.EndDateTime.Value:yyyy-MM-dd})."
                    });
                }
            }
        }

        // Weak protocol detection
        if (result.PrimaryProtocol == AuthenticationProtocol.PasswordBased)
        {
            insights.Add(new GovernanceInsight
            {
                Severity = GovernanceSeverity.Warning,
                Category = GovernanceCategory.Protocol,
                Message = "Application uses Password-Based SSO. Consider migrating to SAML or OIDC for improved security."
            });
        }

        if (result.PrimaryProtocol == AuthenticationProtocol.LinkedSignOn)
        {
            insights.Add(new GovernanceInsight
            {
                Severity = GovernanceSeverity.Info,
                Category = GovernanceCategory.Protocol,
                Message = "Application uses Linked Sign-On. No SSO configuration is managed by Entra ID."
            });
        }

        // Implicit flow detection
        if (data.EnableIdTokenIssuance == true && data.EnableAccessTokenIssuance == true)
        {
            insights.Add(new GovernanceInsight
            {
                Severity = GovernanceSeverity.Warning,
                Category = GovernanceCategory.Security,
                Message = "Both ID token and access token issuance are enabled. Consider disabling implicit flow for security."
            });
        }

        // No SSO configured
        if (string.IsNullOrEmpty(data.PreferredSingleSignOnMode))
        {
            insights.Add(new GovernanceInsight
            {
                Severity = GovernanceSeverity.Warning,
                Category = GovernanceCategory.Configuration,
                Message = "No SSO mode configured. Application may not support single sign-on."
            });
        }

        return insights;
    }
}

/// <summary>
/// Generates actionable governance insights from analysis results.
/// </summary>
internal sealed class InsightGenerator : IInsightGenerator
{
    public List<GovernanceInsight> Generate(ProtocolAnalysisResult result, DetectionData data)
    {
        var insights = new List<GovernanceInsight>();

        // Multiple high-confidence protocols detected
        var highConfidenceCount = result.DetectedProtocols.Count(d => d.Confidence == ProtocolConfidence.High);
        if (highConfidenceCount > 1)
        {
            var protocols = string.Join(", ", result.DetectedProtocols
                .Where(d => d.Confidence == ProtocolConfidence.High)
                .Select(d => d.Protocol));

            insights.Add(new GovernanceInsight
            {
                Severity = GovernanceSeverity.Warning,
                Category = GovernanceCategory.Configuration,
                Message = $"Multiple protocols detected with high confidence: {protocols}. This may indicate misconfiguration."
            });
        }

        // No protocol detected
        if (result.PrimaryProtocol == AuthenticationProtocol.Unknown)
        {
            insights.Add(new GovernanceInsight
            {
                Severity = GovernanceSeverity.Warning,
                Category = GovernanceCategory.Configuration,
                Message = "No authentication protocol could be detected. Review application SSO configuration."
            });
        }

        // HTTP redirect URIs detected (security concern)
        var httpUris = data.RedirectUris.Where(r => r.StartsWith("http://", StringComparison.OrdinalIgnoreCase)).ToList();
        if (httpUris.Count > 0)
        {
            insights.Add(new GovernanceInsight
            {
                Severity = GovernanceSeverity.Warning,
                Category = GovernanceCategory.Security,
                Message = $"Application has {httpUris.Count} HTTP (non-HTTPS) redirect URI(s). HTTPS is recommended for security."
            });
        }

        return insights;
    }
}

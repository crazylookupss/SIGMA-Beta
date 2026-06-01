using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Application.ProtocolAnalysis.Engine;
using Xunit;

namespace SIGMA.Application.Tests.ProtocolAnalysis;

public sealed class GovernanceAnalyzerTests
{
    private readonly GovernanceAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_WithExpiredCertificate_ReturnsCriticalInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.OpenIdConnect,
            DetectedProtocols = [],
        };
        var data = new DetectionData
        {
            KeyCredentials =
            [
                new DetectionCredential
                {
                    DisplayName = "ExpiredCert",
                    EndDateTime = DateTimeOffset.UtcNow.AddDays(-10),
                }
            ]
        };

        var insights = _analyzer.Analyze(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Critical &&
            i.Category == GovernanceCategory.Certificate &&
            i.Message.Contains("ExpiredCert"));
    }

    [Fact]
    public void Analyze_WithExpiringCertificate_ReturnsWarningInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.OpenIdConnect,
            DetectedProtocols = [],
        };
        var data = new DetectionData
        {
            KeyCredentials =
            [
                new DetectionCredential
                {
                    DisplayName = "ExpiringCert",
                    EndDateTime = DateTimeOffset.UtcNow.AddDays(15),
                }
            ]
        };

        var insights = _analyzer.Analyze(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Warning &&
            i.Category == GovernanceCategory.Certificate &&
            i.Message.Contains("ExpiringCert"));
    }

    [Fact]
    public void Analyze_WithPasswordBasedProtocol_ReturnsWarningInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.PasswordBased,
            DetectedProtocols = [],
        };
        var data = new DetectionData();

        var insights = _analyzer.Analyze(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Warning &&
            i.Category == GovernanceCategory.Protocol &&
            i.Message.Contains("Password-Based"));
    }

    [Fact]
    public void Analyze_WithLinkedSignOnProtocol_ReturnsInfoInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.LinkedSignOn,
            DetectedProtocols = [],
        };
        var data = new DetectionData();

        var insights = _analyzer.Analyze(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Info &&
            i.Category == GovernanceCategory.Protocol &&
            i.Message.Contains("Linked Sign-On"));
    }

    [Fact]
    public void Analyze_WithImplicitFlowEnabled_ReturnsWarningInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.OpenIdConnect,
            DetectedProtocols = [],
        };
        var data = new DetectionData
        {
            EnableIdTokenIssuance = true,
            EnableAccessTokenIssuance = true,
        };

        var insights = _analyzer.Analyze(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Warning &&
            i.Category == GovernanceCategory.Security &&
            i.Message.Contains("implicit flow"));
    }

    [Fact]
    public void Analyze_WithNoSsoConfigured_ReturnsWarningInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.Unknown,
            DetectedProtocols = [],
        };
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = null,
        };

        var insights = _analyzer.Analyze(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Warning &&
            i.Category == GovernanceCategory.Configuration &&
            i.Message.Contains("No SSO mode"));
    }

    [Fact]
    public void Analyze_WithSecureConfig_ReturnsNoInsights()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.Saml,
            DetectedProtocols = [],
        };
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "saml",
            EnableIdTokenIssuance = false,
            EnableAccessTokenIssuance = false,
            KeyCredentials =
            [
                new DetectionCredential
                {
                    DisplayName = "ValidCert",
                    EndDateTime = DateTimeOffset.UtcNow.AddYears(1),
                }
            ]
        };

        var insights = _analyzer.Analyze(result, data);

        Assert.Empty(insights);
    }
}

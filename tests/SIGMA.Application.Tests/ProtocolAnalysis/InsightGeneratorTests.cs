using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Application.ProtocolAnalysis.Engine;
using Xunit;

namespace SIGMA.Application.Tests.ProtocolAnalysis;

public sealed class InsightGeneratorTests
{
    private readonly InsightGenerator _generator = new();

    [Fact]
    public void Generate_WithMultipleHighConfidenceProtocols_ReturnsWarningInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.Saml,
            DetectedProtocols =
            [
                new ProtocolDetection
                {
                    Protocol = AuthenticationProtocol.Saml,
                    Confidence = ProtocolConfidence.High,
                    Score = 100,
                },
                new ProtocolDetection
                {
                    Protocol = AuthenticationProtocol.OpenIdConnect,
                    Confidence = ProtocolConfidence.High,
                    Score = 90,
                }
            ]
        };
        var data = new DetectionData();

        var insights = _generator.Generate(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Warning &&
            i.Category == GovernanceCategory.Configuration &&
            i.Message.Contains("Multiple protocols"));
    }

    [Fact]
    public void Generate_WithUnknownProtocol_ReturnsWarningInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.Unknown,
            DetectedProtocols = [],
        };
        var data = new DetectionData();

        var insights = _generator.Generate(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Warning &&
            i.Category == GovernanceCategory.Configuration &&
            i.Message.Contains("No authentication protocol"));
    }

    [Fact]
    public void Generate_WithHttpRedirectUris_ReturnsWarningInsight()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.OAuth2,
            DetectedProtocols =
            [
                new ProtocolDetection
                {
                    Protocol = AuthenticationProtocol.OAuth2,
                    Confidence = ProtocolConfidence.High,
                    Score = 50,
                }
            ]
        };
        var data = new DetectionData
        {
            RedirectUris = ["http://app.example.com/callback"],
        };

        var insights = _generator.Generate(result, data);

        Assert.Contains(insights, i =>
            i.Severity == GovernanceSeverity.Warning &&
            i.Category == GovernanceCategory.Security &&
            i.Message.Contains("HTTP"));
    }

    [Fact]
    public void Generate_WithSecureConfig_ReturnsNoInsights()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.Saml,
            DetectedProtocols =
            [
                new ProtocolDetection
                {
                    Protocol = AuthenticationProtocol.Saml,
                    Confidence = ProtocolConfidence.High,
                    Score = 100,
                }
            ]
        };
        var data = new DetectionData
        {
            RedirectUris = ["https://app.example.com/callback"],
        };

        var insights = _generator.Generate(result, data);

        Assert.Empty(insights);
    }

    [Fact]
    public void Generate_WithSingleHighConfidenceProtocol_ReturnsNoWarning()
    {
        var result = new ProtocolAnalysisResult
        {
            PrimaryProtocol = AuthenticationProtocol.OpenIdConnect,
            DetectedProtocols =
            [
                new ProtocolDetection
                {
                    Protocol = AuthenticationProtocol.OpenIdConnect,
                    Confidence = ProtocolConfidence.High,
                    Score = 100,
                }
            ]
        };
        var data = new DetectionData
        {
            RedirectUris = ["https://app.example.com/callback"],
        };

        var insights = _generator.Generate(result, data);

        Assert.DoesNotContain(insights, i =>
            i.Message.Contains("Multiple protocols"));
    }
}

using SIGMA.Application.Features.ProtocolAnalysis.Detectors;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using Xunit;

namespace SIGMA.Application.Tests.ProtocolAnalysis;

public sealed class WsFedProtocolDetectorTests
{
    [Fact]
    public async Task DetectAsync_WithExplicitWsFedMode_ReturnsHighConfidence()
    {
        var detector = new WsFedProtocolDetector();
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "wsFed",
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(AuthenticationProtocol.WsFed, result.Protocol);
        Assert.Equal(ProtocolConfidence.High, result.Confidence);
        Assert.Contains(result.Evidence, e => e.Field == "PreferredSingleSignOnMode");
    }

    [Fact]
    public async Task DetectAsync_WithAdfsIdentifierUri_AddsWsFedEvidence()
    {
        var detector = new WsFedProtocolDetector();
        var data = new DetectionData
        {
            IdentifierUris = ["https://adfs.example.com/adfs"],
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "IdentifierUris");
    }

    [Fact]
    public async Task DetectAsync_WithFederationTag_AddsTagEvidence()
    {
        var detector = new WsFedProtocolDetector();
        var data = new DetectionData
        {
            ApplicationTags = ["WindowsAzureActiveDirectoryCustomSingleSignOnApplication"],
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "Tags");
    }

    [Fact]
    public async Task DetectAsync_WithHttpsIdentifierNoSamlNoOidc_AddsPotentialEvidence()
    {
        var detector = new WsFedProtocolDetector();
        var data = new DetectionData
        {
            IdentifierUris = ["https://app.example.com"],
            SamlMetadataUrl = null,
            EnableIdTokenIssuance = null,
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "IdentifierUris");
    }

    [Fact]
    public async Task DetectAsync_WithNoWsFedSignals_ReturnsZeroScore()
    {
        var detector = new WsFedProtocolDetector();
        var data = new DetectionData();

        var result = await detector.DetectAsync(data);

        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Score);
        Assert.Empty(result.Evidence);
    }

    [Fact]
    public async Task DetectAsync_WithHomePageUrlAndOtherSignals_AddsHomepageEvidence()
    {
        var detector = new WsFedProtocolDetector();
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "wsFed",
            HomePageUrl = "https://app.example.com",
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "HomePageUrl");
    }

    [Fact]
    public async Task DetectAsync_WithMultipleSignals_ReturnsHighConfidence()
    {
        var detector = new WsFedProtocolDetector();
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "wsfed",
            IdentifierUris = ["https://adfs.example.com/adfs"],
            ApplicationTags = ["WindowsAzureActiveDirectoryCustomSingleSignOnApplication"],
            HomePageUrl = "https://app.example.com",
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(ProtocolConfidence.High, result.Confidence);
        Assert.True(result.Score >= 50);
    }
}

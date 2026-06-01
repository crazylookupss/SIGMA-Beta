using SIGMA.Application.Features.ProtocolAnalysis.Detectors;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using Xunit;

namespace SIGMA.Application.Tests.ProtocolAnalysis;

public sealed class OidcProtocolDetectorTests
{
    [Fact]
    public async Task DetectAsync_WithExplicitOidcMode_ReturnsHighConfidenceDetection()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "oidc",
            EnableIdTokenIssuance = true,
            RedirectUris = ["https://app.example.com/callback"],
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(AuthenticationProtocol.OpenIdConnect, result.Protocol);
        Assert.Equal(ProtocolConfidence.High, result.Confidence);
        Assert.True(result.Score >= 60);
        Assert.Contains(result.Evidence, e => e.Field == "PreferredSingleSignOnMode");
    }

    [Fact]
    public async Task DetectAsync_WithIdTokenIssuanceEnabled_ReturnsMediumConfidence()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            EnableIdTokenIssuance = true,
            RedirectUris = ["https://app.example.com/callback"],
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(AuthenticationProtocol.OpenIdConnect, result.Protocol);
        Assert.Contains(result.Evidence, e => e.Field == "EnableIdTokenIssuance");
    }

    [Fact]
    public async Task DetectAsync_WithNoOidcSignals_ReturnsZeroScore()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData();

        var result = await detector.DetectAsync(data);

        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Score);
        Assert.Empty(result.Evidence);
    }

    [Fact]
    public async Task DetectAsync_WithPublicClientRedirects_AddsRedirectEvidence()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            PublicClientRedirectUris = ["msal://redirect"],
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "PublicClient.RedirectUris");
    }

    [Fact]
    public async Task DetectAsync_WithGroupMembershipClaims_AddsClaimsEvidence()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            GroupMembershipClaims = "SecurityGroup",
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "GroupMembershipClaims");
    }

    [Fact]
    public async Task DetectAsync_WithOptionalClaims_AddsClaimsEvidence()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            OptionalClaims = new { },
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "OptionalClaims");
    }

    [Fact]
    public async Task DetectAsync_WithPreAuthorizedApps_AddsAppEvidence()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            PreAuthorizedApplicationsCount = 3,
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "PreAuthorizedApplications");
    }

    [Fact]
    public async Task DetectAsync_WithLogoutUrl_AddsLogoutEvidence()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            LogoutUrl = "https://app.example.com/logout",
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "LogoutUrl");
    }

    [Fact]
    public async Task DetectAsync_WithMultipleSignals_ReturnsHighConfidence()
    {
        var detector = new OidcProtocolDetector();
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "oidc",
            EnableIdTokenIssuance = true,
            EnableAccessTokenIssuance = true,
            RedirectUris = ["https://app.example.com/callback"],
            IdentifierUris = ["https://app.example.com"],
            RequestedAccessTokenVersion = 2,
            GroupMembershipClaims = "SecurityGroup",
            LogoutUrl = "https://app.example.com/logout",
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(ProtocolConfidence.High, result.Confidence);
        Assert.True(result.Score >= 90);
        Assert.True(result.Evidence.Count >= 5);
    }
}

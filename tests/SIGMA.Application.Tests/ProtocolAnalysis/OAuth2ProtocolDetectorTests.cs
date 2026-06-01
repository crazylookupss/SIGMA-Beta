using SIGMA.Application.Features.ProtocolAnalysis.Detectors;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using Xunit;

namespace SIGMA.Application.Tests.ProtocolAnalysis;

public sealed class OAuth2ProtocolDetectorTests
{
    [Fact]
    public async Task DetectAsync_WithExposedScopes_ReturnsDetected()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            ExposedScopeValues = ["User.Read", "Mail.Read"],
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(AuthenticationProtocol.OAuth2, result.Protocol);
        Assert.Contains(result.Evidence, e => e.Field == "Api.Oauth2PermissionScopes");
    }

    [Fact]
    public async Task DetectAsync_WithCustomSchemeRedirects_ReturnsHighConfidence()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            RedirectUris = ["com.myapp://callback"],
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(AuthenticationProtocol.OAuth2, result.Protocol);
        Assert.Contains(result.Evidence, e => e.Field == "RedirectUris");
    }

    [Fact]
    public async Task DetectAsync_WithDelegatedPermissions_AddsPermissionEvidence()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            RequiredResourceAccess =
            [
                new DetectionResourceAccess
                {
                    ResourceAppId = "00000000-0000-0000-0000-000000000000",
                    ResourceAccess =
                    [
                        new DetectionAccess { Id = "scope-id", Type = "Scope" }
                    ]
                }
            ]
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "RequiredResourceAccess");
    }

    [Fact]
    public async Task DetectAsync_WithAppPermissions_AddsPermissionEvidence()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            RequiredResourceAccess =
            [
                new DetectionResourceAccess
                {
                    ResourceAppId = "00000000-0000-0000-0000-000000000000",
                    ResourceAccess =
                    [
                        new DetectionAccess { Id = "role-id", Type = "Role" }
                    ]
                }
            ]
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "RequiredResourceAccess");
    }

    [Fact]
    public async Task DetectAsync_WithV1AccessTokenVersion_AddsApiEvidence()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            RequestedAccessTokenVersion = 1,
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "RequestedAccessTokenVersion");
    }

    [Fact]
    public async Task DetectAsync_WithAcceptMappedClaims_AddsApiEvidence()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            AcceptMappedClaims = true,
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "AcceptMappedClaims");
    }

    [Fact]
    public async Task DetectAsync_WithNoOAuth2Signals_ReturnsZeroScore()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData();

        var result = await detector.DetectAsync(data);

        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Score);
        Assert.Empty(result.Evidence);
    }

    [Fact]
    public async Task DetectAsync_WithKnownClientApps_AddsAppEvidence()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            KnownClientApplicationsCount = 2,
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Contains(result.Evidence, e => e.Field == "KnownClientApplications");
    }

    [Fact]
    public async Task DetectAsync_WithMultipleSignals_ReturnsHighConfidence()
    {
        var detector = new OAuth2ProtocolDetector();
        var data = new DetectionData
        {
            ExposedScopeValues = ["User.Read"],
            RequiredResourceAccess =
            [
                new DetectionResourceAccess
                {
                    ResourceAppId = "00000000-0000-0000-0000-000000000000",
                    ResourceAccess =
                    [
                        new DetectionAccess { Id = "scope-id", Type = "Scope" },
                        new DetectionAccess { Id = "role-id", Type = "Role" }
                    ]
                }
            ],
            RequestedAccessTokenVersion = 1,
            AcceptMappedClaims = true,
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(ProtocolConfidence.High, result.Confidence);
        Assert.True(result.Score >= 50);
    }
}

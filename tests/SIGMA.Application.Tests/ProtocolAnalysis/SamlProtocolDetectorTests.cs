using SIGMA.Application.Features.ProtocolAnalysis.Detectors;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using Xunit;

namespace SIGMA.Application.Tests.ProtocolAnalysis;

public sealed class SamlProtocolDetectorTests
{
    [Fact]
    public async Task DetectAsync_WithExplicitSamlSignals_ReturnsHighConfidenceDetection()
    {
        var detector = new SamlProtocolDetector();
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "saml",
            SamlMetadataUrl = "https://idp.example.com/metadata",
            IdentifierUris = ["urn:example:service-provider"],
            RedirectUris = ["https://app.example.com/saml/acs"],
            ServicePrincipalNames = ["urn:example:service-provider"],
            KeyCredentials =
            [
                new DetectionCredential
                {
                    Type = "AsymmetricX509Cert",
                    Usage = "Verify",
                    EndDateTime = DateTimeOffset.UtcNow.AddYears(1)
                }
            ]
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.IsDetected);
        Assert.Equal(AuthenticationProtocol.Saml, result.Protocol);
        Assert.Equal(ProtocolConfidence.High, result.Confidence);
        Assert.True(result.Score >= 90);
        Assert.Contains(result.Evidence, e => e.Field == "PreferredSingleSignOnMode");
        Assert.Contains(result.Evidence, e => e.Field == "SamlMetadataUrl");
    }

    [Fact]
    public async Task DetectAsync_WithExpiredCertificate_DoesNotReturnNegativeScore()
    {
        var detector = new SamlProtocolDetector();
        var data = new DetectionData
        {
            PreferredSingleSignOnMode = "saml",
            KeyCredentials =
            [
                new DetectionCredential
                {
                    Type = "AsymmetricX509Cert",
                    Usage = "Verify",
                    EndDateTime = DateTimeOffset.UtcNow.AddDays(-1)
                }
            ]
        };

        var result = await detector.DetectAsync(data);

        Assert.True(result.Score >= 0);
        Assert.Contains(result.Evidence, e => e.Category == "CertificateHealth");
    }
}

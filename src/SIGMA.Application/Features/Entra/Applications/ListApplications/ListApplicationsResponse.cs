namespace SIGMA.Application.Features.Entra.Applications.ListApplications;

public sealed record ListApplicationsResponse
{
    public string Id { get; init; } = string.Empty;
    public string? AppId { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public string? SignInAudience { get; init; }
    public string? PublisherDomain { get; init; }
    public List<string> IdentifierUris { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public ListApplicationsVerifiedPublisher? VerifiedPublisher { get; init; }
    public ListApplicationsCertification? Certification { get; init; }
}

public sealed record ListApplicationsVerifiedPublisher(
    string? DisplayName,
    string? VerifiedPublisherId,
    DateTimeOffset? AddedDateTime);

public sealed record ListApplicationsCertification(
    bool? IsPublisherAttested,
    bool? IsCertifiedByMicrosoft,
    DateTimeOffset? LastCertificationDateTime,
    DateTimeOffset? CertificationExpirationDateTime,
    string? CertificationDetailsUrl);

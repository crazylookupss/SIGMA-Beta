namespace SIGMA.Domain.Entities;

public sealed record EntraApplication
{
    public string Id { get; init; } = string.Empty;
    public string? AppId { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public string? SignInAudience { get; init; }
    public string? PublisherDomain { get; init; }
    public List<string> IdentifierUris { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public VerifiedPublisherDto? VerifiedPublisher { get; init; }
    public CertificationDto? Certification { get; init; }
}

public sealed record VerifiedPublisherDto(
    string? DisplayName,
    string? VerifiedPublisherId,
    DateTimeOffset? AddedDateTime);

public sealed record CertificationDto(
    bool? IsPublisherAttested,
    bool? IsCertifiedByMicrosoft,
    DateTimeOffset? LastCertificationDateTime,
    DateTimeOffset? CertificationExpirationDateTime,
    string? CertificationDetailsUrl);

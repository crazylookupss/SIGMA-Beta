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

    // New properties from standard Microsoft Graph Application entity
    public DateTimeOffset? DeletedDateTime { get; init; }
    public bool? IsFallbackPublicClient { get; init; }
    public string? ApplicationTemplateId { get; init; }
    public string? CreatedByAppId { get; init; }
    public string? DisabledByMicrosoftStatus { get; init; }
    public bool? IsDeviceOnlyAuthSupported { get; init; }
    public string? GroupMembershipClaims { get; init; }
    public object? OptionalClaims { get; init; }
    public List<object> AddIns { get; init; } = [];
    public string? SamlMetadataUrl { get; init; }
    public string? TokenEncryptionKeyId { get; init; }
    public ApiApplicationDto? Api { get; init; }
    public List<object> AppRoles { get; init; } = [];
    public PublicClientApplicationDto? PublicClient { get; init; }
    public InformationalUrlDto? Info { get; init; }
    public List<object> KeyCredentials { get; init; } = [];
    public ParentalControlSettingsDto? ParentalControlSettings { get; init; }
    public List<object> PasswordCredentials { get; init; } = [];
    public List<object> RequiredResourceAccess { get; init; } = [];
    public WebApplicationDto? Web { get; init; }
 
    // Enriched properties for governance analysis
    public int? OwnersCount { get; init; }
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

public sealed record ApiApplicationDto(
    int? RequestedAccessTokenVersion,
    bool? AcceptMappedClaims,
    List<string> KnownClientApplications,
    List<object> Oauth2PermissionScopes,
    List<PreAuthorizedAppDto> PreAuthorizedApplications);

public sealed record PreAuthorizedAppDto(
    string? AppId,
    List<string> PermissionScopes);

public sealed record PublicClientApplicationDto(
    List<string> RedirectUris);

public sealed record InformationalUrlDto(
    string? TermsOfServiceUrl,
    string? SupportUrl,
    string? PrivacyStatementUrl,
    string? MarketingUrl,
    string? LogoUrl);

public sealed record ParentalControlSettingsDto(
    List<string> CountriesBlockedForMinors,
    string? LegalAgeGroupRule);

public sealed record WebApplicationDto(
    List<string> RedirectUris,
    string? HomePageUrl,
    string? LogoutUrl,
    ImplicitGrantSettingsDto? ImplicitGrantSettings);

public sealed record ImplicitGrantSettingsDto(
    bool? EnableIdTokenIssuance,
    bool? EnableAccessTokenIssuance);

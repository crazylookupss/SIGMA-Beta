namespace SIGMA.Application.Features.Entra.Applications.GetApplication;

public sealed record GetApplicationResponse
{
    public string Id { get; init; } = string.Empty;
    public string? AppId { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public string? SignInAudience { get; init; }
    public string? PublisherDomain { get; init; }
    public List<string> IdentifierUris { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public GetApplicationVerifiedPublisher? VerifiedPublisher { get; init; }
    public GetApplicationCertification? Certification { get; init; }

    // New properties
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
    public GetApplicationApi? Api { get; init; }
    public List<object> AppRoles { get; init; } = [];
    public GetApplicationPublicClient? PublicClient { get; init; }
    public GetApplicationInfo? Info { get; init; }
    public List<object> KeyCredentials { get; init; } = [];
    public GetApplicationParentalControlSettings? ParentalControlSettings { get; init; }
    public List<object> PasswordCredentials { get; init; } = [];
    public List<object> RequiredResourceAccess { get; init; } = [];
    public GetApplicationWeb? Web { get; init; }
}

public sealed record GetApplicationVerifiedPublisher(
    string? DisplayName,
    string? VerifiedPublisherId,
    DateTimeOffset? AddedDateTime);

public sealed record GetApplicationCertification(
    bool? IsPublisherAttested,
    bool? IsCertifiedByMicrosoft,
    DateTimeOffset? LastCertificationDateTime,
    DateTimeOffset? CertificationExpirationDateTime,
    string? CertificationDetailsUrl);

public sealed record GetApplicationApi(
    int? RequestedAccessTokenVersion,
    bool? AcceptMappedClaims,
    List<object> KnownClientApplications,
    List<object> Oauth2PermissionScopes,
    List<object> PreAuthorizedApplications);

public sealed record GetApplicationPublicClient(
    List<string> RedirectUris);

public sealed record GetApplicationInfo(
    string? TermsOfServiceUrl,
    string? SupportUrl,
    string? PrivacyStatementUrl,
    string? MarketingUrl,
    string? LogoUrl);

public sealed record GetApplicationParentalControlSettings(
    List<string> CountriesBlockedForMinors,
    string? LegalAgeGroupRule);

public sealed record GetApplicationWeb(
    List<string> RedirectUris,
    string? HomePageUrl,
    string? LogoutUrl,
    GetApplicationImplicitGrantSettings? ImplicitGrantSettings);

public sealed record GetApplicationImplicitGrantSettings(
    bool? EnableIdTokenIssuance,
    bool? EnableAccessTokenIssuance);

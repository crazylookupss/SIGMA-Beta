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
    public ListApplicationsApi? Api { get; init; }
    public List<object> AppRoles { get; init; } = [];
    public ListApplicationsPublicClient? PublicClient { get; init; }
    public ListApplicationsInfo? Info { get; init; }
    public List<object> KeyCredentials { get; init; } = [];
    public ListApplicationsParentalControlSettings? ParentalControlSettings { get; init; }
    public List<object> PasswordCredentials { get; init; } = [];
    public List<object> RequiredResourceAccess { get; init; } = [];
    public ListApplicationsWeb? Web { get; init; }
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

public sealed record ListApplicationsApi(
    int? RequestedAccessTokenVersion,
    bool? AcceptMappedClaims,
    List<object> KnownClientApplications,
    List<object> Oauth2PermissionScopes,
    List<object> PreAuthorizedApplications);

public sealed record ListApplicationsPublicClient(
    List<string> RedirectUris);

public sealed record ListApplicationsInfo(
    string? TermsOfServiceUrl,
    string? SupportUrl,
    string? PrivacyStatementUrl,
    string? MarketingUrl,
    string? LogoUrl);

public sealed record ListApplicationsParentalControlSettings(
    List<string> CountriesBlockedForMinors,
    string? LegalAgeGroupRule);

public sealed record ListApplicationsWeb(
    List<string> RedirectUris,
    string? HomePageUrl,
    string? LogoutUrl,
    ListApplicationsImplicitGrantSettings? ImplicitGrantSettings);

public sealed record ListApplicationsImplicitGrantSettings(
    bool? EnableIdTokenIssuance,
    bool? EnableAccessTokenIssuance);

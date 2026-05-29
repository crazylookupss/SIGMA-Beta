namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public sealed record DetectionData
{
    public string ServicePrincipalId { get; init; } = string.Empty;
    public string? AppId { get; init; }
    public string? DisplayName { get; init; }
    public string? PreferredSingleSignOnMode { get; init; }
    public List<string> ServicePrincipalTags { get; init; } = [];
    public List<string> NotificationEmailAddresses { get; init; } = [];

    public List<string> IdentifierUris { get; init; } = [];
    public List<string> ApplicationTags { get; init; } = [];
    public string? SamlMetadataUrl { get; init; }
    public string? TokenEncryptionKeyId { get; init; }
    public List<string> RedirectUris { get; init; } = [];
    public List<string> PublicClientRedirectUris { get; init; } = [];
    public string? HomePageUrl { get; init; }
    public string? LogoutUrl { get; init; }
    public bool? EnableIdTokenIssuance { get; init; }
    public bool? EnableAccessTokenIssuance { get; init; }
    public int? RequestedAccessTokenVersion { get; init; }
    public bool? AcceptMappedClaims { get; init; }
    public bool? IsFallbackPublicClient { get; init; }
    public List<string> ExposedScopeValues { get; init; } = [];
    public List<DetectionCredential> KeyCredentials { get; init; } = [];
    public List<DetectionPasswordCredential> PasswordCredentials { get; init; } = [];
    public List<DetectionResourceAccess> RequiredResourceAccess { get; init; } = [];
}

public sealed record DetectionCredential
{
    public string? KeyId { get; init; }
    public string? DisplayName { get; init; }
    public string? Type { get; init; }
    public string? Usage { get; init; }
}

public sealed record DetectionPasswordCredential
{
    public string? KeyId { get; init; }
    public string? DisplayName { get; init; }
}

public sealed record DetectionResourceAccess
{
    public string? ResourceAppId { get; init; }
    public List<DetectionAccess> ResourceAccess { get; init; } = [];
}

public sealed record DetectionAccess
{
    public string? Id { get; init; }
    public string? Type { get; init; }
}

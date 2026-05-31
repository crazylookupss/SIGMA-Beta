namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalSsoConfig;

public sealed record GetServicePrincipalSsoConfigResponse
{
    public bool IsConfigured { get; init; }
    public string PreferredSingleSignOnMode { get; init; } = string.Empty;
    public string? DetectedPrimaryProtocol { get; init; }
    public string? SamlMetadataUrl { get; init; }
    public string? EntityId { get; init; }
    public List<string> ReplyUrls { get; init; } = [];
    public string? SignOnUrl { get; init; }
    public string? LogoutUrl { get; init; }
    public string? HomePageUrl { get; init; }
    public List<SsoCertificateDto> Certificates { get; init; } = [];
    public string? AuthorizationEndpoint { get; init; }
    public string? TokenEndpoint { get; init; }
    public string? Issuer { get; init; }
    public string? FederationMetadataUrl { get; init; }
    public string? LoginUrl { get; init; }
    public string? MicrosoftEntraIdentifier { get; init; }
    public string? TenantId { get; init; }

    // Claims configuration
    public string? GroupMembershipClaims { get; init; }
    public List<string> OptionalClaims { get; init; } = [];
    public List<SamlClaimDto> SamlClaims { get; init; } = [];

    // OIDC grant types
    public bool? EnableIdTokenIssuance { get; init; }
    public bool? EnableAccessTokenIssuance { get; init; }
}

public sealed record SsoCertificateDto
{
    public string? KeyId { get; init; }
    public string? DisplayName { get; init; }
    public string? Thumbprint { get; init; }
    public string? Type { get; init; }
    public string? Usage { get; init; }
    public DateTimeOffset? StartDateTime { get; init; }
    public DateTimeOffset? EndDateTime { get; init; }
}

public sealed record SamlClaimDto
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public bool IsOptional { get; init; }
}

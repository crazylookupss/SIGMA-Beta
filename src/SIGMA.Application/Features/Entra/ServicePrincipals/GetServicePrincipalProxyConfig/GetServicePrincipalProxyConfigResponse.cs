namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalProxyConfig;

public sealed record GetServicePrincipalProxyConfigResponse
{
    public bool IsConfigured { get; init; }
    public string? ExternalUrl { get; init; }
    public string? InternalUrl { get; init; }
    public string? PreAuthentication { get; init; }
    public bool IsTranslationUrlEnabled { get; init; }
    public bool TranslateUrlsInBody { get; init; }
    public bool TranslateLinksInBody { get; init; }
    public bool VerifyDomainCertificates { get; init; }
}

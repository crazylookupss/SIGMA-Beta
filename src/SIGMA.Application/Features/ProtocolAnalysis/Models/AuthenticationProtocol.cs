namespace SIGMA.Application.Features.ProtocolAnalysis.Models;

public enum AuthenticationProtocol
{
    Saml,
    OpenIdConnect,
    OAuth2,
    WsFed,
    HeaderBased,
    PasswordBased,
    LinkedSignOn,
    ScimProvisioning,
    Unknown
}

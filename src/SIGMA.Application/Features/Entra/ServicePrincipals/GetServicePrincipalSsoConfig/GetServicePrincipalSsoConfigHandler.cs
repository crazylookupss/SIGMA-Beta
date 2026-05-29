using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalSsoConfig;

internal sealed class GetServicePrincipalSsoConfigHandler(
    IGraphClientService graphClient)
    : IQueryHandler<GetServicePrincipalSsoConfigQuery, Result<GetServicePrincipalSsoConfigResponse>>
{
    public async Task<Result<GetServicePrincipalSsoConfigResponse>> Handle(
        GetServicePrincipalSsoConfigQuery query, CancellationToken cancellationToken)
    {
        var configResult = await graphClient.GetServicePrincipalSsoConfigAsync(
            query.ServicePrincipalId, cancellationToken);

        if (configResult.IsFailure)
            return configResult.Error!;

        var cfg = configResult.Value!;
        return Result.Success(new GetServicePrincipalSsoConfigResponse
        {
            PreferredSingleSignOnMode = cfg.PreferredSingleSignOnMode,
            SamlMetadataUrl = cfg.SamlMetadataUrl,
            EntityId = cfg.EntityId,
            ReplyUrls = cfg.ReplyUrls,
            SignOnUrl = cfg.SignOnUrl,
            LogoutUrl = cfg.LogoutUrl,
            HomePageUrl = cfg.HomePageUrl,
            AuthorizationEndpoint = cfg.AuthorizationEndpoint,
            TokenEndpoint = cfg.TokenEndpoint,
            Issuer = cfg.Issuer,
            FederationMetadataUrl = cfg.FederationMetadataUrl,
            LoginUrl = cfg.LoginUrl,
            MicrosoftEntraIdentifier = cfg.MicrosoftEntraIdentifier,
            TenantId = cfg.TenantId,
            Certificates = cfg.Certificates.Select(c => new SsoCertificateDto
            {
                KeyId = c.KeyId,
                DisplayName = c.DisplayName,
                Thumbprint = c.Thumbprint,
                Type = c.Type,
                Usage = c.Usage,
                StartDateTime = c.StartDateTime,
                EndDateTime = c.EndDateTime,
            }).ToList(),
        });
    }
}

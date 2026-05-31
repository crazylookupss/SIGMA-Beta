using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.ServicePrincipals.GetServicePrincipalProxyConfig;

internal sealed class GetServicePrincipalProxyConfigHandler(IGraphClientService graphClient)
    : IQueryHandler<GetServicePrincipalProxyConfigQuery, Result<GetServicePrincipalProxyConfigResponse>>
{
    public async Task<Result<GetServicePrincipalProxyConfigResponse>> Handle(
        GetServicePrincipalProxyConfigQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetServicePrincipalProxyConfigAsync(
            query.ServicePrincipalId, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var config = result.Value!;
        return Result.Success(new GetServicePrincipalProxyConfigResponse
        {
            IsConfigured = config.IsConfigured,
            ExternalUrl = config.ExternalUrl,
            InternalUrl = config.InternalUrl,
            PreAuthentication = config.PreAuthentication,
            IsTranslationUrlEnabled = config.IsTranslationUrlEnabled,
            TranslateUrlsInBody = config.TranslateUrlsInBody,
            TranslateLinksInBody = config.TranslateLinksInBody,
            VerifyDomainCertificates = config.VerifyDomainCertificates,
        });
    }
}

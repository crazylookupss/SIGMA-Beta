using SIGMA.Application.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.ProtocolAnalysis.Queries.GetProtocolAnalysis;

internal sealed class GetProtocolAnalysisHandler(
    IGraphClientService graphClient,
    IProtocolAnalysisEngine engine)
    : IQueryHandler<GetProtocolAnalysisQuery, Result<ProtocolAnalysisResult>>
{
    public async Task<Result<ProtocolAnalysisResult>> Handle(
        GetProtocolAnalysisQuery query, CancellationToken ct)
    {
        var spResult = await graphClient.GetServicePrincipalByIdAsync(query.ServicePrincipalId, null, ct);
        if (spResult.IsFailure)
            return spResult.Error!;

        var sp = spResult.Value!;

        var data = new DetectionData
        {
            ServicePrincipalId = sp.Id,
            AppId = sp.AppId,
            DisplayName = sp.DisplayName,
            PreferredSingleSignOnMode = sp.PreferredSingleSignOnMode,
            ServicePrincipalTags = sp.Tags,
            NotificationEmailAddresses = sp.NotificationEmailAddresses,
        };

        if (!string.IsNullOrEmpty(sp.AppId))
        {
            var appResult = await graphClient.GetApplicationByAppIdAsync(sp.AppId, ct);
            if (appResult.IsSuccess)
            {
                var app = appResult.Value!;
                data = data with
                {
                    IdentifierUris = app.IdentifierUris,
                    ApplicationTags = app.Tags,
                    SamlMetadataUrl = app.SamlMetadataUrl,
                    TokenEncryptionKeyId = app.TokenEncryptionKeyId,
                    RedirectUris = app.RedirectUris,
                    PublicClientRedirectUris = app.PublicClientRedirectUris,
                    HomePageUrl = app.HomePageUrl,
                    LogoutUrl = app.LogoutUrls?.FirstOrDefault(),
                    EnableIdTokenIssuance = app.EnableIdTokenIssuance,
                    EnableAccessTokenIssuance = app.EnableAccessTokenIssuance,
                    RequestedAccessTokenVersion = app.RequestedAccessTokenVersion,
                    AcceptMappedClaims = app.AcceptMappedClaims,
                    IsFallbackPublicClient = app.IsFallbackPublicClient,
                    ExposedScopeValues = app.Oauth2PermissionScopeValues,
                    KeyCredentials = app.KeyCredentials.Select(k => new DetectionCredential
                    {
                        KeyId = k.KeyId,
                        DisplayName = k.DisplayName,
                        Type = k.Type,
                        Usage = k.Usage,
                    }).ToList(),
                    RequiredResourceAccess = app.RequiredResourceAccess.Select(r => new DetectionResourceAccess
                    {
                        ResourceAppId = r.ResourceAppId,
                        ResourceAccess = r.DelegatedPermissions.Select(p => new DetectionAccess
                        {
                            Id = p,
                            Type = "Scope"
                        }).Concat(r.ApplicationPermissions.Select(p => new DetectionAccess
                        {
                            Id = p,
                            Type = "Role"
                        })).ToList()
                    }).ToList(),
                };
            }
        }

        var result = await engine.AnalyzeAsync(data, ct);
        return Result.Success(result);
    }
}

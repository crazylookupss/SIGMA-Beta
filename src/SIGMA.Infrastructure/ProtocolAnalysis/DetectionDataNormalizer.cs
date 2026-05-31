using SIGMA.Application.Features.ProtocolAnalysis.Models;
using SIGMA.Application.ProtocolAnalysis.Pipeline;

namespace SIGMA.Infrastructure.ProtocolAnalysis;

/// <summary>
/// Normalizes collected Graph data into DetectionData for protocol analysis.
/// </summary>
internal sealed class DetectionDataNormalizer : INormalizationService
{
    public DetectionData Normalize(CollectedData data)
    {
        var sp = data.ServicePrincipal;
        var app = data.ApplicationDetails;

        var detectionData = new DetectionData
        {
            ServicePrincipalId = sp.Id,
            AppId = sp.AppId,
            DisplayName = sp.DisplayName,
            PreferredSingleSignOnMode = sp.PreferredSingleSignOnMode,
            ServicePrincipalTags = sp.Tags,
            NotificationEmailAddresses = sp.NotificationEmailAddresses,
            CustomSingleSignOnUrl = sp.CustomSingleSignOnUrl,
            ServicePrincipalNames = sp.ServicePrincipalNames,
            LoginUrl = sp.LoginUrl,
            PreferredTokenSigningKeyThumbprint = sp.PreferredTokenSigningKeyThumbprint,
        };

        if (app is not null)
        {
            detectionData = detectionData with
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
                    StartDateTime = k.StartDateTime,
                    EndDateTime = k.EndDateTime,
                    Thumbprint = k.Thumbprint,
                }).ToList(),
                PasswordCredentials = app.PasswordCredentials.Select(p => new DetectionPasswordCredential
                {
                    KeyId = p.KeyId,
                    DisplayName = p.DisplayName,
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Hint = p.Hint,
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
                GroupMembershipClaims = app.GroupMembershipClaims,
                OptionalClaims = app.OptionalClaims,
                PreAuthorizedApplicationsCount = app.PreAuthorizedApplications.Count,
                KnownClientApplicationsCount = app.KnownClientApplications.Count,
            };
        }

        return detectionData;
    }
}

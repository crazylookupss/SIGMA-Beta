using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Applications.ListApplications;

internal sealed class ListApplicationsHandler(IGraphClientService graphClient)
    : IQueryHandler<ListApplicationsQuery, Result<PagedResponse<ListApplicationsResponse>>>
{
    public async Task<Result<PagedResponse<ListApplicationsResponse>>> Handle(
        ListApplicationsQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetApplicationsAsync(
            query.Select, query.Filter, query.Top, query.Skip, query.Count, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var paged = result.Value!;
        var response = new PagedResponse<ListApplicationsResponse>
        {
            Data = paged.Data.Select(app => new ListApplicationsResponse
            {
                Id = app.Id,
                AppId = app.AppId,
                DisplayName = app.DisplayName,
                CreatedDateTime = app.CreatedDateTime,
                SignInAudience = app.SignInAudience,
                PublisherDomain = app.PublisherDomain,
                IdentifierUris = app.IdentifierUris,
                Tags = app.Tags,
                VerifiedPublisher = app.VerifiedPublisher is null ? null : new ListApplicationsVerifiedPublisher(
                    app.VerifiedPublisher.DisplayName,
                    app.VerifiedPublisher.VerifiedPublisherId,
                    app.VerifiedPublisher.AddedDateTime),
                Certification = app.Certification is null ? null : new ListApplicationsCertification(
                    app.Certification.IsPublisherAttested,
                    app.Certification.IsCertifiedByMicrosoft,
                    app.Certification.LastCertificationDateTime,
                    app.Certification.CertificationExpirationDateTime,
                    app.Certification.CertificationDetailsUrl),

                // New mappings
                DeletedDateTime = app.DeletedDateTime,
                IsFallbackPublicClient = app.IsFallbackPublicClient,
                ApplicationTemplateId = app.ApplicationTemplateId,
                CreatedByAppId = app.CreatedByAppId,
                DisabledByMicrosoftStatus = app.DisabledByMicrosoftStatus,
                IsDeviceOnlyAuthSupported = app.IsDeviceOnlyAuthSupported,
                GroupMembershipClaims = app.GroupMembershipClaims,
                OptionalClaims = app.OptionalClaims,
                AddIns = app.AddIns,
                SamlMetadataUrl = app.SamlMetadataUrl,
                TokenEncryptionKeyId = app.TokenEncryptionKeyId,
                Api = app.Api is null ? null : new ListApplicationsApi(
                    app.Api.RequestedAccessTokenVersion,
                    app.Api.AcceptMappedClaims,
                    app.Api.KnownClientApplications,
                    app.Api.Oauth2PermissionScopes,
                    app.Api.PreAuthorizedApplications.Select(pa => new ListApplicationsPreAuthorizedApp(pa.AppId, pa.PermissionScopes)).ToList()),
                AppRoles = app.AppRoles,
                PublicClient = app.PublicClient is null ? null : new ListApplicationsPublicClient(
                    app.PublicClient.RedirectUris),
                Info = app.Info is null ? null : new ListApplicationsInfo(
                    app.Info.TermsOfServiceUrl,
                    app.Info.SupportUrl,
                    app.Info.PrivacyStatementUrl,
                    app.Info.MarketingUrl,
                    app.Info.LogoUrl),
                KeyCredentials = app.KeyCredentials,
                ParentalControlSettings = app.ParentalControlSettings is null ? null : new ListApplicationsParentalControlSettings(
                    app.ParentalControlSettings.CountriesBlockedForMinors,
                    app.ParentalControlSettings.LegalAgeGroupRule),
                PasswordCredentials = app.PasswordCredentials,
                RequiredResourceAccess = app.RequiredResourceAccess,
                Web = app.Web is null ? null : new ListApplicationsWeb(
                    app.Web.RedirectUris,
                    app.Web.HomePageUrl,
                    app.Web.LogoutUrl,
                    app.Web.ImplicitGrantSettings is null ? null : new ListApplicationsImplicitGrantSettings(
                        app.Web.ImplicitGrantSettings.EnableIdTokenIssuance,
                        app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance))
            }).ToList(),
            NextLink = paged.NextLink,
            Count = paged.Count
        };

        return Result.Success(response);
    }
}

using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Applications.GetApplication;

internal sealed class GetApplicationHandler(IGraphClientService graphClient)
    : IQueryHandler<GetApplicationQuery, Result<GetApplicationResponse>>
{
    public async Task<Result<GetApplicationResponse>> Handle(
        GetApplicationQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetApplicationByIdAsync(
            query.Id, query.Select, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var app = result.Value!;
        var response = new GetApplicationResponse
        {
            Id = app.Id,
            AppId = app.AppId,
            DisplayName = app.DisplayName,
            CreatedDateTime = app.CreatedDateTime,
            SignInAudience = app.SignInAudience,
            PublisherDomain = app.PublisherDomain,
            IdentifierUris = app.IdentifierUris,
            Tags = app.Tags,
            VerifiedPublisher = app.VerifiedPublisher is null ? null : new GetApplicationVerifiedPublisher(
                app.VerifiedPublisher.DisplayName,
                app.VerifiedPublisher.VerifiedPublisherId,
                app.VerifiedPublisher.AddedDateTime),
            Certification = app.Certification is null ? null : new GetApplicationCertification(
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
            Api = app.Api is null ? null : new GetApplicationApi(
                app.Api.RequestedAccessTokenVersion,
                app.Api.AcceptMappedClaims,
                app.Api.KnownClientApplications,
                app.Api.Oauth2PermissionScopes,
                app.Api.PreAuthorizedApplications),
            AppRoles = app.AppRoles,
            PublicClient = app.PublicClient is null ? null : new GetApplicationPublicClient(
                app.PublicClient.RedirectUris),
            Info = app.Info is null ? null : new GetApplicationInfo(
                app.Info.TermsOfServiceUrl,
                app.Info.SupportUrl,
                app.Info.PrivacyStatementUrl,
                app.Info.MarketingUrl,
                app.Info.LogoUrl),
            KeyCredentials = app.KeyCredentials,
            ParentalControlSettings = app.ParentalControlSettings is null ? null : new GetApplicationParentalControlSettings(
                app.ParentalControlSettings.CountriesBlockedForMinors,
                app.ParentalControlSettings.LegalAgeGroupRule),
            PasswordCredentials = app.PasswordCredentials,
            RequiredResourceAccess = app.RequiredResourceAccess,
            Web = app.Web is null ? null : new GetApplicationWeb(
                app.Web.RedirectUris,
                app.Web.HomePageUrl,
                app.Web.LogoutUrl,
                app.Web.ImplicitGrantSettings is null ? null : new GetApplicationImplicitGrantSettings(
                    app.Web.ImplicitGrantSettings.EnableIdTokenIssuance,
                    app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance))
        };

        return Result.Success(response);
    }
}

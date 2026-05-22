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
                app.Certification.CertificationDetailsUrl)
        };

        return Result.Success(response);
    }
}

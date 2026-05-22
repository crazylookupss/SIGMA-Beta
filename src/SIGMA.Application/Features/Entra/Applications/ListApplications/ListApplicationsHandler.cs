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
                    app.Certification.CertificationDetailsUrl)
            }).ToList(),
            NextLink = paged.NextLink,
            Count = paged.Count
        };

        return Result.Success(response);
    }
}

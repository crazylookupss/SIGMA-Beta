namespace SIGMA.Application.Features.Entra.Groups.GetAccessReviews;

public sealed record GroupAccessReviewResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public int? ReviewersCount { get; init; }
}

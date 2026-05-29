namespace SIGMA.Domain.Entities;

public sealed record EntraGroupAccessReview
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public int? ReviewersCount { get; init; }
    public int? DecisionsCount { get; init; }
    public DateTimeOffset? NextReviewDate { get; init; }
}

namespace SIGMA.Application.Features.Entra.Groups.ListGroups;

public sealed record ListGroupsResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? Mail { get; init; }
    public bool? MailEnabled { get; init; }
    public bool? SecurityEnabled { get; init; }
    public string? MailNickname { get; init; }
    public List<string> GroupTypes { get; init; } = [];
    public string? Visibility { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public int? MemberCount { get; init; }

    // Basic classifiers available in lists
    public string? MembershipType { get; init; }
    public string? Source { get; init; }
    public string? Type { get; init; }
}

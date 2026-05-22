namespace SIGMA.Application.Features.Entra.Groups.GetGroup;

public sealed record GetGroupResponse
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

    // New enriched properties
    public string? MembershipType { get; init; }
    public string? Source { get; init; }
    public string? Type { get; init; }
    public int? TotalDirectMembers { get; init; }
    public int? DirectUsers { get; init; }
    public int? DirectGroups { get; init; }
    public int? DirectDevices { get; init; }
    public int? DirectOthers { get; init; }
    public int? GroupMembershipsCount { get; init; }
    public int? OwnersCount { get; init; }
    public int? TotalMembers { get; init; }
}

namespace SIGMA.Application.Features.Entra.Groups.GetMembers;

public sealed record GroupMemberResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserPrincipalName { get; init; }
    public string MemberType { get; init; } = "User";
    public DateTimeOffset? CreatedDateTime { get; init; }
}

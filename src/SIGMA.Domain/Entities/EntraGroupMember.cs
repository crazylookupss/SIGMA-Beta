namespace SIGMA.Domain.Entities;

public sealed record EntraGroupMember
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserPrincipalName { get; init; }
    public string MemberType { get; init; } = "User";
    public DateTimeOffset? CreatedDateTime { get; init; }
}

namespace SIGMA.Domain.Entities;

public sealed record EntraGroupOwner
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserPrincipalName { get; init; }
    public string OwnerType { get; init; } = "User";
}

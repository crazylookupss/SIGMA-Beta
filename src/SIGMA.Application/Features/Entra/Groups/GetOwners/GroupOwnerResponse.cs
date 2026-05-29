namespace SIGMA.Application.Features.Entra.Groups.GetOwners;

public sealed record GroupOwnerResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserPrincipalName { get; init; }
    public string OwnerType { get; init; } = "User";
}

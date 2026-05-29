namespace SIGMA.Domain.Entities;

public sealed record EntraGroupApplication
{
    public string Id { get; init; } = string.Empty;
    public string? AppId { get; init; }
    public string? DisplayName { get; init; }
    public bool? AccountEnabled { get; init; }
    public string? ResourceId { get; init; }
    public string? AppRoleId { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
}

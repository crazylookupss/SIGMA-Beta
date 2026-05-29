namespace SIGMA.Application.Features.Entra.Groups.GetApplications;

public sealed record GroupApplicationResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? AppRoleId { get; init; }
    public string? ResourceId { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
}

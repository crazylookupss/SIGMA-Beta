namespace SIGMA.Domain.Entities;

public sealed record EntraServicePrincipal
{
    public string Id { get; init; } = string.Empty;
    public string? AppId { get; init; }
    public string? DisplayName { get; init; }
    public string? AppDisplayName { get; init; }
    public string? ServicePrincipalType { get; init; }
    public bool? AccountEnabled { get; init; }
    public string? PublisherName { get; init; }
    public string? SignInAudience { get; init; }
    public List<string> Tags { get; init; } = [];
    public string? AppOwnerOrganizationId { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }

    // Telemetry fields
    public string SignInStatus { get; init; } = "Active";
    public int UsersCount { get; init; }
    public DateTimeOffset? LastSignIn { get; init; }
}


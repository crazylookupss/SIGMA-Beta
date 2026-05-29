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
    public bool AppRoleAssignmentRequired { get; init; }
    public string? PreferredSingleSignOnMode { get; init; }

    // Advanced properties
    public string? AppDescription { get; init; }
    public List<string> NotificationEmailAddresses { get; init; } = [];
    public List<object> AppRoles { get; init; } = [];
    public List<object> KeyCredentials { get; init; } = [];
    public List<object> PasswordCredentials { get; init; } = [];
    public int? AssignedUserCount { get; init; }
    public int? AssignedGroupCount { get; init; }
}


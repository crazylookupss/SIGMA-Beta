using SIGMA.Application.Common;
using SIGMA.Domain.Common;
using SIGMA.Domain.Entities;

namespace SIGMA.Application.Abstractions;

public interface IGraphClientService
{
    Task<Result<PagedResponse<EntraUser>>> GetUsersAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraUser>> GetUserByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<EntraGroup>>> GetGroupsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraGroup>> GetGroupByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<EntraServicePrincipal>>> GetServicePrincipalsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraServicePrincipal>> GetServicePrincipalByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);

    Task<Result<PagedResponse<EntraApplication>>> GetApplicationsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default);

    Task<Result<EntraApplication>> GetApplicationByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default);

    Task<Result<EntraTenant>> GetTenantDetailsAsync(
        CancellationToken cancellationToken = default);

    Task<List<SignInHistoryEntry>> GetSignInHistoryAsync(
        int days = 30, CancellationToken cancellationToken = default);

    Task<List<EntraAppAssignment>> GetServicePrincipalAssignmentsAsync(
        string servicePrincipalId, CancellationToken cancellationToken = default);

    Task<List<EntraAppOwner>> GetServicePrincipalOwnersAsync(
        string servicePrincipalId, CancellationToken cancellationToken = default);

    Task<List<SignInHistoryEntry>> GetSignInsForServicePrincipalAsync(
        string appId, int days = 7, CancellationToken cancellationToken = default);

    Task<Result<EntraApplicationDetails>> GetApplicationByAppIdAsync(
        string appId, CancellationToken cancellationToken = default);

    // App Registrations Dashboard methods
    Task<List<EntraAppOwner>> GetApplicationOwnersAsync(
        string applicationId, CancellationToken cancellationToken = default);

    Task<List<ServicePrincipalRef>> GetServicePrincipalsForApplicationAsync(
        string appId, CancellationToken cancellationToken = default);

    Task<ApplicationStatistics> GetApplicationStatisticsAsync(
        CancellationToken cancellationToken = default);

    Task<AppCredentialHealth> GetApplicationCredentialsAsync(
        string applicationId, CancellationToken cancellationToken = default);

    // App Registration Details methods
    Task<List<AuditLogEntry>> GetApplicationAuditLogsAsync(
        string appId, int top = 50, CancellationToken cancellationToken = default);

    Task<Result<string>> GetApplicationManifestAsync(
        string applicationId, CancellationToken cancellationToken = default);

    Task<Result<ServicePrincipalRef>> GetServicePrincipalForApplicationAsync(
        string appId, CancellationToken cancellationToken = default);
}

public sealed record AuditLogEntry
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset? CreatedDateTime { get; init; }
    public string? UserDisplayName { get; init; }
    public string? UserPrincipalName { get; init; }
    public string? AppDisplayName { get; init; }
    public string? Status { get; init; }
    public string? IpAddress { get; init; }
    public string? Location { get; init; }
    public string? Device { get; init; }
    public string? ClientAppUsed { get; init; }
    public string? RiskLevel { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed record ServicePrincipalRef
{
    public string Id { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public bool? AccountEnabled { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
}

public sealed record ApplicationStatistics
{
    public int TotalAppRegistrations { get; init; }
    public int ActiveApplications { get; init; }
    public int WarningApplications { get; init; }
    public int HighRiskApplications { get; init; }
    public int TotalServicePrincipals { get; init; }
    public List<StatusStat> StatusDistribution { get; init; } = [];
    public List<ProtocolStat> ProtocolDistribution { get; init; } = [];
    public List<TopAppBySpCount> TopApplicationsBySp { get; init; } = [];
    public int OwnerlessApps { get; init; }
    public int MultiTenantApps { get; init; }
    public int ExternalPublisherApps { get; init; }
    public int AppsWithExpiringSecrets { get; init; }
    public int AppsWithExpiredSecrets { get; init; }
}

public sealed record StatusStat
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record ProtocolStat
{
    public string Protocol { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record TopAppBySpCount
{
    public string AppId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public int ServicePrincipalCount { get; init; }
}

public sealed record AppCredentialHealth
{
    public List<CredentialInfo> Certificates { get; init; } = [];
    public List<CredentialInfo> Secrets { get; init; } = [];
    public bool HasExpiredCredentials { get; init; }
    public bool HasExpiringCredentials { get; init; }
    public int CredentialsExpiringWithin30Days { get; init; }
    public int CredentialsExpired { get; init; }
}

public sealed record CredentialInfo
{
    public string? KeyId { get; init; }
    public string? DisplayName { get; init; }
    public string? Type { get; init; }
    public string? Usage { get; init; }
    public DateTimeOffset? StartDateTime { get; init; }
    public DateTimeOffset? EndDateTime { get; init; }
    public string? Hint { get; init; }
    public bool IsExpired { get; init; }
    public bool IsExpiringSoon { get; init; }
    public int DaysUntilExpiry { get; init; }
}

public sealed record SignInHistoryEntry(DateTimeOffset? CreatedDateTime);

public sealed record EntraAppAssignment
{
    public string Id { get; init; } = string.Empty;
    public string PrincipalId { get; init; } = string.Empty;
    public string PrincipalDisplayName { get; init; } = string.Empty;
    public string PrincipalType { get; init; } = string.Empty;
    public string? AppRoleId { get; init; }
    public string? AppRoleValue { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
}

public sealed record EntraAppOwner
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? UserPrincipalName { get; init; }
    public string OwnerType { get; init; } = "User";
}

public sealed record EntraApplicationDetails
{
    public string Id { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? SignInAudience { get; init; }
    public string? PublisherDomain { get; init; }
    public List<string> IdentifierUris { get; init; } = [];
    public List<string> RedirectUris { get; init; } = [];
    public List<string> LogoutUrls { get; init; } = [];
    public List<EntraAppPermission> RequiredResourceAccess { get; init; } = [];
    public List<EntraAppRole> AppRoles { get; init; } = [];
    public List<EntraKeyCredential> KeyCredentials { get; init; } = [];
    public List<EntraPasswordCredential> PasswordCredentials { get; init; } = [];
    public string? HomePageUrl { get; init; }
    public string? TermsOfServiceUrl { get; init; }
    public string? PrivacyStatementUrl { get; init; }
    public string? SupportUrl { get; init; }
    public string? SamlMetadataUrl { get; init; }
    public string? ApplicationTemplateId { get; init; }
    public string? LogoUrl { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public VerifiedPublisherInfo? VerifiedPublisher { get; init; }
    public CertificationInfo? Certification { get; init; }
}

public sealed record EntraAppPermission
{
    public string ResourceAppId { get; init; } = string.Empty;
    public string? ResourceDisplayName { get; init; }
    public List<string> DelegatedPermissions { get; init; } = [];
    public List<string> ApplicationPermissions { get; init; } = [];
}

public sealed record EntraAppRole
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public string? Value { get; init; }
    public bool IsEnabled { get; init; }
    public List<string> AllowedMemberTypes { get; init; } = [];
}

public sealed record EntraKeyCredential
{
    public string? KeyId { get; init; }
    public string? DisplayName { get; init; }
    public string? Type { get; init; }
    public string? Usage { get; init; }
    public DateTimeOffset? StartDateTime { get; init; }
    public DateTimeOffset? EndDateTime { get; init; }
}

public sealed record EntraPasswordCredential
{
    public string? KeyId { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset? StartDateTime { get; init; }
    public DateTimeOffset? EndDateTime { get; init; }
    public string? Hint { get; init; }
}

public sealed record VerifiedPublisherInfo
{
    public string? DisplayName { get; init; }
    public string? VerifiedPublisherId { get; init; }
    public DateTimeOffset? AddedDateTime { get; init; }
}

public sealed record CertificationInfo
{
    public bool? IsPublisherAttested { get; init; }
    public bool? IsCertifiedByMicrosoft { get; init; }
    public DateTimeOffset? LastCertificationDateTime { get; init; }
    public DateTimeOffset? CertificationExpirationDateTime { get; init; }
    public string? CertificationDetailsUrl { get; init; }
}


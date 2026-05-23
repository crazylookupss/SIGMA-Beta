using SIGMA.Domain.Entities;

namespace SIGMA.Application.Features.Entra.Users.GetUser;

public sealed record GetUserResponse
{
    public string Id { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? GivenName { get; init; }
    public string? Surname { get; init; }
    public string? UserPrincipalName { get; init; }
    public List<ObjectIdentityDto> Identities { get; init; } = [];
    public string? UserType { get; init; }
    public string? CreationType { get; init; }
    public DateTimeOffset? CreatedDateTime { get; init; }
    public List<AssignedLicenseDto> AssignedLicenses { get; init; } = [];
    public string? PreferredLanguage { get; init; }
    public DateTimeOffset? SignInSessionsValidFromDateTime { get; init; }
    public DateTimeOffset? LastPasswordChangeDateTime { get; init; }
    public string? ExternalUserState { get; init; }
    public DateTimeOffset? ExternalUserStateChangeDateTime { get; init; }
    public string? PasswordPolicies { get; init; }
    public PasswordProfileDto? PasswordProfile { get; init; }
    public AuthorizationInfoDto? AuthorizationInfo { get; init; }

    // Job Information
    public string? JobTitle { get; init; }
    public string? CompanyName { get; init; }
    public string? Department { get; init; }
    public string? EmployeeId { get; init; }
    public string? EmployeeType { get; init; }
    public DateTimeOffset? EmployeeHireDate { get; init; }
    public EmployeeOrgDataDto? EmployeeOrgData { get; init; }
    public string? OfficeLocation { get; init; }
    public UserManagerDto? Manager { get; init; }
    public List<UserSponsorDto> Sponsors { get; init; } = [];

    // Contact Information
    public string? StreetAddress { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? BusinessPhone { get; init; }
    public List<string> BusinessPhones { get; init; } = [];
    public string? MobilePhone { get; init; }
    public string? Mail { get; init; }
    public List<string> OtherMails { get; init; } = [];
    public List<string> ProxyAddresses { get; init; } = [];
    public string? FaxNumber { get; init; }
    public List<string> ImAddresses { get; init; } = [];
    public string? MailNickname { get; init; }

    // Parental controls
    public string? AgeGroup { get; init; }
    public string? ConsentProvidedForMinor { get; init; }
    public string? LegalAgeGroupClassification { get; init; }

    // Settings
    public bool? AccountEnabled { get; init; }
    public string? UsageLocation { get; init; }
    public string? PreferredDataLocation { get; init; }

    // On-premises
    public bool? OnPremisesSyncEnabled { get; init; }
    public DateTimeOffset? OnPremisesLastSyncDateTime { get; init; }
    public string? OnPremisesDistinguishedName { get; init; }
    public OnPremisesExtensionAttributesDto? OnPremisesExtensionAttributes { get; init; }
    public string? OnPremisesImmutableId { get; init; }
    public List<OnPremisesProvisioningErrorDto> OnPremisesProvisioningErrors { get; init; } = [];
    public string? OnPremisesSamAccountName { get; init; }
    public string? OnPremisesSecurityIdentifier { get; init; }
    public string? OnPremisesUserPrincipalName { get; init; }
    public string? OnPremisesDomainName { get; init; }
}

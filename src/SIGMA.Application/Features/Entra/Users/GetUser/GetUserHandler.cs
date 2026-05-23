using SIGMA.Application.Abstractions;
using SIGMA.Domain.Common;

namespace SIGMA.Application.Features.Entra.Users.GetUser;

internal sealed class GetUserHandler(IGraphClientService graphClient)
    : IQueryHandler<GetUserQuery, Result<GetUserResponse>>
{
    public async Task<Result<GetUserResponse>> Handle(
        GetUserQuery query, CancellationToken cancellationToken)
    {
        var result = await graphClient.GetUserByIdAsync(
            query.Id, query.Select, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var user = result.Value!;
        var response = new GetUserResponse
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            GivenName = user.GivenName,
            Surname = user.Surname,
            UserPrincipalName = user.UserPrincipalName,
            Identities = user.Identities,
            UserType = user.UserType,
            CreationType = user.CreationType,
            CreatedDateTime = user.CreatedDateTime,
            AssignedLicenses = user.AssignedLicenses,
            PreferredLanguage = user.PreferredLanguage,
            SignInSessionsValidFromDateTime = user.SignInSessionsValidFromDateTime,
            LastPasswordChangeDateTime = user.LastPasswordChangeDateTime,
            ExternalUserState = user.ExternalUserState,
            ExternalUserStateChangeDateTime = user.ExternalUserStateChangeDateTime,
            PasswordPolicies = user.PasswordPolicies,
            PasswordProfile = user.PasswordProfile,
            AuthorizationInfo = user.AuthorizationInfo,

            // Job Information
            JobTitle = user.JobTitle,
            CompanyName = user.CompanyName,
            Department = user.Department,
            EmployeeId = user.EmployeeId,
            EmployeeType = user.EmployeeType,
            EmployeeHireDate = user.EmployeeHireDate,
            EmployeeOrgData = user.EmployeeOrgData,
            OfficeLocation = user.OfficeLocation,
            Manager = user.Manager,
            Sponsors = user.Sponsors,

            // Contact Information
            StreetAddress = user.StreetAddress,
            City = user.City,
            State = user.State,
            PostalCode = user.PostalCode,
            Country = user.Country,
            BusinessPhone = user.BusinessPhone,
            BusinessPhones = user.BusinessPhones,
            MobilePhone = user.MobilePhone,
            Mail = user.Mail,
            OtherMails = user.OtherMails,
            ProxyAddresses = user.ProxyAddresses,
            FaxNumber = user.FaxNumber,
            ImAddresses = user.ImAddresses,
            MailNickname = user.MailNickname,

            // Parental controls
            AgeGroup = user.AgeGroup,
            ConsentProvidedForMinor = user.ConsentProvidedForMinor,
            LegalAgeGroupClassification = user.LegalAgeGroupClassification,

            // Settings
            AccountEnabled = user.AccountEnabled,
            UsageLocation = user.UsageLocation,
            PreferredDataLocation = user.PreferredDataLocation,

            // On-premises
            OnPremisesSyncEnabled = user.OnPremisesSyncEnabled,
            OnPremisesLastSyncDateTime = user.OnPremisesLastSyncDateTime,
            OnPremisesDistinguishedName = user.OnPremisesDistinguishedName,
            OnPremisesExtensionAttributes = user.OnPremisesExtensionAttributes,
            OnPremisesImmutableId = user.OnPremisesImmutableId,
            OnPremisesProvisioningErrors = user.OnPremisesProvisioningErrors,
            OnPremisesSamAccountName = user.OnPremisesSamAccountName,
            OnPremisesSecurityIdentifier = user.OnPremisesSecurityIdentifier,
            OnPremisesUserPrincipalName = user.OnPremisesUserPrincipalName,
            OnPremisesDomainName = user.OnPremisesDomainName,
        };

        return Result.Success(response);
    }
}

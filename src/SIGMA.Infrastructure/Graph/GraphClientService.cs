using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
using SIGMA.Domain.Common;
using SIGMA.Domain.Entities;
using SIGMA.Infrastructure.Authentication;

namespace SIGMA.Infrastructure.Graph;

internal sealed class GraphClientService : IGraphClientService, IDisposable
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0/";
    private const string DefaultUserSelect = "id,displayName,givenName,surname,userPrincipalName,identities,userType,creationType,createdDateTime,assignedLicenses,preferredLanguage,signInSessionsValidFromDateTime,lastPasswordChangeDateTime,externalUserState,externalUserStateChangeDateTime,passwordPolicies,passwordProfile,authorizationInfo,jobTitle,companyName,department,employeeId,employeeType,employeeHireDate,employeeOrgData,officeLocation,streetAddress,city,state,postalCode,country,businessPhones,mobilePhone,mail,otherMails,proxyAddresses,faxNumber,imAddresses,mailNickname,ageGroup,consentProvidedForMinor,legalAgeGroupClassification,accountEnabled,usageLocation,preferredDataLocation,onPremisesSyncEnabled,onPremisesLastSyncDateTime,onPremisesDistinguishedName,onPremisesExtensionAttributes,onPremisesImmutableId,onPremisesProvisioningErrors,onPremisesSamAccountName,onPremisesSecurityIdentifier,onPremisesUserPrincipalName,onPremisesDomainName";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;
    private AccessToken? _currentToken;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public GraphClientService(EntraAuthConfiguration config)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(GraphBaseUrl) };
        _credential = new ClientSecretCredential(
            config.TenantId, config.ClientId, config.ClientSecret,
            new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            });
        _scopes = config.Scopes;
    }

    public async Task<Result<PagedResponse<EntraUser>>> GetUsersAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<GraphUser, EntraUser>(
            "users", MapUser, select, filter, top, skip, count, cancellationToken);
    }

    public async Task<Result<EntraUser>> GetUserByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        var escapedId = Uri.EscapeDataString(id);
        var selectFields = string.IsNullOrWhiteSpace(select) ? DefaultUserSelect : select;

        var userResult = await GetSingleAsync<GraphUser, EntraUser>(
            $"users/{escapedId}", MapUser, selectFields, cancellationToken);

        if (userResult.IsFailure)
            return userResult;

        var user = userResult.Value!;

        // Fetch manager and sponsors in parallel
        var managerTask = GetSingleNavigationAsync<GraphManagerDto>($"users/{escapedId}/manager?$select=id,displayName,userPrincipalName", cancellationToken);
        var sponsorsTask = GetCollectionListAsync<GraphSponsorDto>($"users/{escapedId}/sponsors?$select=id,displayName,userPrincipalName", cancellationToken);

        try
        {
            await Task.WhenAll(managerTask, sponsorsTask);

            var manager = managerTask.Result;
            var sponsors = sponsorsTask.Result;

            return user with
            {
                Manager = manager is null ? null : new UserManagerDto(manager.Id, manager.DisplayName, manager.UserPrincipalName),
                Sponsors = sponsors.Select(s => new UserSponsorDto(s.Id, s.DisplayName, s.UserPrincipalName)).ToList()
            };
        }
        catch
        {
            return user;
        }
    }

    public async Task<Result<PagedResponse<EntraGroup>>> GetGroupsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<GraphGroup, EntraGroup>(
            "groups", MapGroup, select, filter, top, skip, count, cancellationToken);
    }

    public async Task<Result<EntraGroup>> GetGroupByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        var escapedId = Uri.EscapeDataString(id);
        var groupResult = await GetSingleAsync<GraphGroup, EntraGroup>(
            $"groups/{escapedId}", MapGroup, select, cancellationToken);

        if (groupResult.IsFailure)
            return groupResult;

        var group = groupResult.Value!;

        // Fire concurrent relationship queries for enriched Entra Overview metrics
        var membersTask = GetCollectionListAsync<MemberDto>($"groups/{escapedId}/members?$select=id", cancellationToken);
        var ownersTask = GetCollectionListAsync<OwnerDto>($"groups/{escapedId}/owners?$select=id", cancellationToken);
        var memberOfTask = GetCollectionListAsync<MemberOfDto>($"groups/{escapedId}/memberOf?$select=id", cancellationToken);
        var transitiveTask = GetCollectionListAsync<TransitiveMemberDto>($"groups/{escapedId}/transitiveMembers?$select=id", cancellationToken);

        try
        {
            await Task.WhenAll(membersTask, ownersTask, memberOfTask, transitiveTask);

            var members = membersTask.Result;
            var directUsers = members.Count(m => m.OdataType == "#microsoft.graph.user" || m.OdataType == "microsoft.graph.user");
            var directGroups = members.Count(m => m.OdataType == "#microsoft.graph.group" || m.OdataType == "microsoft.graph.group");
            var directDevices = members.Count(m => m.OdataType == "#microsoft.graph.device" || m.OdataType == "microsoft.graph.device");
            var directOthers = members.Count - (directUsers + directGroups + directDevices);

            return group with
            {
                TotalDirectMembers = members.Count,
                DirectUsers = directUsers,
                DirectGroups = directGroups,
                DirectDevices = directDevices,
                DirectOthers = directOthers,
                GroupMembershipsCount = memberOfTask.Result.Count,
                OwnersCount = ownersTask.Result.Count,
                TotalMembers = transitiveTask.Result.Count
            };
        }
        catch
        {
            // Fail gracefully to basic group properties if Graph relationship endpoints fail
            return group;
        }
    }

    private async Task<List<T>> GetCollectionListAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            var response = await SendGetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
                return [];

            var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<T>>(JsonOptions, ct);
            return wrapper?.Value ?? [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<T?> GetSingleNavigationAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            var response = await SendGetAsync(path, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<Result<PagedResponse<EntraServicePrincipal>>> GetServicePrincipalsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<GraphServicePrincipalDto, EntraServicePrincipal>(
            "servicePrincipals", MapServicePrincipal, select, filter, top, skip, count, cancellationToken);
    }

    public async Task<Result<EntraServicePrincipal>> GetServicePrincipalByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        return await GetSingleAsync<GraphServicePrincipalDto, EntraServicePrincipal>(
            $"servicePrincipals/{Uri.EscapeDataString(id)}", MapServicePrincipal, select, cancellationToken);
    }

    public async Task<Result<PagedResponse<EntraApplication>>> GetApplicationsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<GraphApplicationDto, EntraApplication>(
            "applications", MapApplication, select, filter, top, skip, count, cancellationToken);
    }

    public async Task<Result<EntraApplication>> GetApplicationByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        return await GetSingleAsync<GraphApplicationDto, EntraApplication>(
            $"applications/{Uri.EscapeDataString(id)}", MapApplication, select, cancellationToken);
    }

    private async Task<Result<PagedResponse<TTarget>>> GetPagedAsync<TSource, TTarget>(
        string path, Func<TSource, TTarget> mapper,
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken ct)
    {
        try
        {
            var url = BuildUrl(path, select, filter, top, skip, count);
            var response = await SendGetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return await HandleErrorResponse(response);

            var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<TSource>>(JsonOptions, ct);

            if (wrapper?.Value == null || wrapper.Value.Count == 0)
                return Error.NotFound($"{typeof(TSource).Name}.NotFound", "No results found.");

            return Result.Success(new PagedResponse<TTarget>
            {
                Data = wrapper.Value.Select(mapper).ToList(),
                NextLink = wrapper.OdataNextLink,
                Count = wrapper.OdataCount,
            });
        }
        catch (HttpRequestException ex)
        {
            return Error.ExternalService("GraphError", ex.Message);
        }
        catch (TaskCanceledException)
        {
            return Error.ExternalService("GraphTimeout", "Request to Microsoft Graph timed out.");
        }
        catch (CredentialUnavailableException ex)
        {
            return Error.Unauthorized("GraphCredentialUnavailable", $"Credential unavailable: {ex.Message}");
        }
        catch (AuthenticationFailedException ex)
        {
            return Error.Unauthorized("GraphAuthFailed", $"Token acquisition failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Error.ExternalService("GraphInvalidResponse", $"Invalid JSON from Graph API: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.ExternalService("GraphUnexpected", $"Unexpected error: {ex.Message}");
        }
    }

    private async Task<Result<TTarget>> GetSingleAsync<TSource, TTarget>(
        string path, Func<TSource, TTarget> mapper,
        string? select, CancellationToken ct)
    {
        try
        {
            var url = BuildUrl(path, select, null, null, null, null);
            var response = await SendGetAsync(url, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Error.NotFound($"{typeof(TSource).Name}.NotFound", $"Resource at '{path}' not found.");

            if (!response.IsSuccessStatusCode)
                return await HandleErrorResponse(response);

            var item = await response.Content.ReadFromJsonAsync<TSource>(JsonOptions, ct);

            if (item == null)
                return Error.NotFound($"{typeof(TSource).Name}.NotFound", $"Resource at '{path}' not found.");

            return Result.Success(mapper(item));
        }
        catch (HttpRequestException ex)
        {
            return Error.ExternalService("GraphError", ex.Message);
        }
        catch (TaskCanceledException)
        {
            return Error.ExternalService("GraphTimeout", "Request to Microsoft Graph timed out.");
        }
        catch (CredentialUnavailableException ex)
        {
            return Error.Unauthorized("GraphCredentialUnavailable", $"Credential unavailable: {ex.Message}");
        }
        catch (AuthenticationFailedException ex)
        {
            return Error.Unauthorized("GraphAuthFailed", $"Token acquisition failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Error.ExternalService("GraphInvalidResponse", $"Invalid JSON from Graph API: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.ExternalService("GraphUnexpected", $"Unexpected error: {ex.Message}");
        }
    }

    private async Task<HttpResponseMessage> SendGetAsync(string url, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_currentToken.HasValue && _currentToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            return _currentToken.Value.Token;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_currentToken.HasValue && _currentToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
                return _currentToken.Value.Token;

            var context = new TokenRequestContext(_scopes);
            _currentToken = await _credential.GetTokenAsync(context, ct);
            return _currentToken.Value.Token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static string BuildUrl(string path, string? select, string? filter, int? top, int? skip, bool? count)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(select))
            queryParams.Add($"$select={Uri.EscapeDataString(select)}");

        if (!string.IsNullOrWhiteSpace(filter))
            queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");

        if (top.HasValue)
            queryParams.Add($"$top={top.Value}");

        if (skip.HasValue)
            queryParams.Add($"$skip={skip.Value}");

        if (count == true)
            queryParams.Add($"$count=true");

        return queryParams.Count > 0 ? $"{path}?{string.Join("&", queryParams)}" : path;
    }

    private static async Task<Error> HandleErrorResponse(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return Error.ExternalService(
            $"GraphError_{(int)response.StatusCode}",
            $"Graph API returned {(int)response.StatusCode}: {body}");
    }

    private static EntraUser MapUser(GraphUser u) => new()
    {
        Id = u.Id ?? string.Empty,
        DisplayName = u.DisplayName,
        GivenName = u.GivenName,
        Surname = u.Surname,
        UserPrincipalName = u.UserPrincipalName,
        Identities = u.Identities ?? [],
        UserType = u.UserType,
        CreationType = u.CreationType,
        CreatedDateTime = u.CreatedDateTime,
        AssignedLicenses = u.AssignedLicenses ?? [],
        PreferredLanguage = u.PreferredLanguage,
        SignInSessionsValidFromDateTime = u.SignInSessionsValidFromDateTime,
        LastPasswordChangeDateTime = u.LastPasswordChangeDateTime,
        ExternalUserState = u.ExternalUserState,
        ExternalUserStateChangeDateTime = u.ExternalUserStateChangeDateTime,
        PasswordPolicies = u.PasswordPolicies,
        PasswordProfile = u.PasswordProfile,
        AuthorizationInfo = u.AuthorizationInfo,

        // Job Information
        JobTitle = u.JobTitle,
        CompanyName = u.CompanyName,
        Department = u.Department,
        EmployeeId = u.EmployeeId,
        EmployeeType = u.EmployeeType,
        EmployeeHireDate = u.EmployeeHireDate,
        EmployeeOrgData = u.EmployeeOrgData,
        OfficeLocation = u.OfficeLocation,

        // Contact Information
        StreetAddress = u.StreetAddress,
        City = u.City,
        State = u.State,
        PostalCode = u.PostalCode,
        Country = u.Country,
        BusinessPhone = u.BusinessPhone ?? u.BusinessPhones?.FirstOrDefault(),
        BusinessPhones = u.BusinessPhones ?? [],
        MobilePhone = u.MobilePhone,
        Mail = u.Mail,
        OtherMails = u.OtherMails ?? [],
        ProxyAddresses = u.ProxyAddresses ?? [],
        FaxNumber = u.FaxNumber,
        ImAddresses = u.ImAddresses ?? [],
        MailNickname = u.MailNickname,

        // Parental controls
        AgeGroup = u.AgeGroup,
        ConsentProvidedForMinor = u.ConsentProvidedForMinor,
        LegalAgeGroupClassification = u.LegalAgeGroupClassification,

        // Settings
        AccountEnabled = u.AccountEnabled,
        UsageLocation = u.UsageLocation,
        PreferredDataLocation = u.PreferredDataLocation,

        // On-premises
        OnPremisesSyncEnabled = u.OnPremisesSyncEnabled,
        OnPremisesLastSyncDateTime = u.OnPremisesLastSyncDateTime,
        OnPremisesDistinguishedName = u.OnPremisesDistinguishedName,
        OnPremisesExtensionAttributes = u.OnPremisesExtensionAttributes,
        OnPremisesImmutableId = u.OnPremisesImmutableId,
        OnPremisesProvisioningErrors = u.OnPremisesProvisioningErrors ?? [],
        OnPremisesSamAccountName = u.OnPremisesSamAccountName,
        OnPremisesSecurityIdentifier = u.OnPremisesSecurityIdentifier,
        OnPremisesUserPrincipalName = u.OnPremisesUserPrincipalName,
        OnPremisesDomainName = u.OnPremisesDomainName,
    };

    private static EntraGroup MapGroup(GraphGroup g)
    {
        var isDynamic = g.GroupTypes?.Contains("DynamicMembership") == true;
        var membershipType = isDynamic ? "Dynamic" : "Assigned";

        var source = g.OnPremisesSyncEnabled == true ? "On-Premises" : "Cloud";

        string type = "Security";
        if (g.GroupTypes?.Contains("Unified") == true)
        {
            type = "Microsoft 365";
        }
        else if (g.MailEnabled == true && g.SecurityEnabled == false)
        {
            type = "Distribution list";
        }
        else if (g.MailEnabled == true && g.SecurityEnabled == true)
        {
            type = "Mail-enabled security";
        }

        return new EntraGroup
        {
            Id = g.Id ?? string.Empty,
            DisplayName = g.DisplayName,
            Description = g.Description,
            Mail = g.Mail,
            MailEnabled = g.MailEnabled,
            SecurityEnabled = g.SecurityEnabled,
            MailNickname = g.MailNickname,
            GroupTypes = g.GroupTypes ?? [],
            Visibility = g.Visibility,
            CreatedDateTime = g.CreatedDateTime,
            
            // New classifiers mapped from Entra ID standard fields
            MembershipType = membershipType,
            Source = source,
            Type = type
        };
    }

    private static EntraServicePrincipal MapServicePrincipal(GraphServicePrincipalDto sp) => new()
    {
        Id = sp.Id ?? string.Empty,
        AppId = sp.AppId,
        DisplayName = sp.DisplayName,
        AppDisplayName = sp.AppDisplayName,
        ServicePrincipalType = sp.ServicePrincipalType,
        AccountEnabled = sp.AccountEnabled,
        PublisherName = sp.PublisherName,
        SignInAudience = sp.SignInAudience,
        Tags = sp.Tags ?? [],
        AppOwnerOrganizationId = sp.AppOwnerOrganizationId,
        CreatedDateTime = sp.CreatedDateTime,
    };

    private static EntraApplication MapApplication(GraphApplicationDto app) => new()
    {
        Id = app.Id ?? string.Empty,
        AppId = app.AppId,
        DisplayName = app.DisplayName,
        CreatedDateTime = app.CreatedDateTime,
        SignInAudience = app.SignInAudience,
        PublisherDomain = app.PublisherDomain,
        IdentifierUris = app.IdentifierUris ?? [],
        Tags = app.Tags ?? [],
        VerifiedPublisher = app.VerifiedPublisher is null ? null : new VerifiedPublisherDto(
            app.VerifiedPublisher.DisplayName,
            app.VerifiedPublisher.VerifiedPublisherId,
            app.VerifiedPublisher.AddedDateTime),
        Certification = app.Certification is null ? null : new CertificationDto(
            app.Certification.IsPublisherAttested,
            app.Certification.IsCertifiedByMicrosoft,
            app.Certification.LastCertificationDateTime,
            app.Certification.CertificationExpirationDateTime,
            app.Certification.CertificationDetailsUrl),

        // New mappings
        DeletedDateTime = app.DeletedDateTime,
        IsFallbackPublicClient = app.IsFallbackPublicClient,
        ApplicationTemplateId = app.ApplicationTemplateId,
        CreatedByAppId = app.CreatedByAppId,
        DisabledByMicrosoftStatus = app.DisabledByMicrosoftStatus,
        IsDeviceOnlyAuthSupported = app.IsDeviceOnlyAuthSupported,
        GroupMembershipClaims = app.GroupMembershipClaims,
        OptionalClaims = app.OptionalClaims,
        AddIns = app.AddIns ?? [],
        SamlMetadataUrl = app.SamlMetadataUrl,
        TokenEncryptionKeyId = app.TokenEncryptionKeyId,
        Api = app.Api is null ? null : new ApiApplicationDto(
            app.Api.RequestedAccessTokenVersion,
            app.Api.AcceptMappedClaims,
            app.Api.KnownClientApplications ?? [],
            app.Api.Oauth2PermissionScopes ?? [],
            app.Api.PreAuthorizedApplications ?? []),
        AppRoles = app.AppRoles ?? [],
        PublicClient = app.PublicClient is null ? null : new PublicClientApplicationDto(
            app.PublicClient.RedirectUris ?? []),
        Info = app.Info is null ? null : new InformationalUrlDto(
            app.Info.TermsOfServiceUrl,
            app.Info.SupportUrl,
            app.Info.PrivacyStatementUrl,
            app.Info.MarketingUrl,
            app.Info.LogoUrl),
        KeyCredentials = app.KeyCredentials ?? [],
        ParentalControlSettings = app.ParentalControlSettings is null ? null : new ParentalControlSettingsDto(
            app.ParentalControlSettings.CountriesBlockedForMinors ?? [],
            app.ParentalControlSettings.LegalAgeGroupRule),
        PasswordCredentials = app.PasswordCredentials ?? [],
        RequiredResourceAccess = app.RequiredResourceAccess ?? [],
        Web = app.Web is null ? null : new WebApplicationDto(
            app.Web.RedirectUris ?? [],
            app.Web.HomePageUrl,
            app.Web.LogoutUrl,
            app.Web.ImplicitGrantSettings is null ? null : new ImplicitGrantSettingsDto(
                app.Web.ImplicitGrantSettings.EnableIdTokenIssuance,
                app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance))
    };

    public void Dispose() => _httpClient.Dispose();

    private sealed record GraphUser
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
        public string? GivenName { get; init; }
        public string? Surname { get; init; }
        public List<ObjectIdentityDto>? Identities { get; init; }
        public string? UserType { get; init; }
        public string? CreationType { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
        public List<AssignedLicenseDto>? AssignedLicenses { get; init; }
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

        // Contact Information
        public string? StreetAddress { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? PostalCode { get; init; }
        public string? Country { get; init; }
        public string? BusinessPhone { get; init; }
        public List<string>? BusinessPhones { get; init; }
        public string? MobilePhone { get; init; }
        public string? Mail { get; init; }
        public List<string>? OtherMails { get; init; }
        public List<string>? ProxyAddresses { get; init; }
        public string? FaxNumber { get; init; }
        public List<string>? ImAddresses { get; init; }
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
        public List<OnPremisesProvisioningErrorDto>? OnPremisesProvisioningErrors { get; init; }
        public string? OnPremisesSamAccountName { get; init; }
        public string? OnPremisesSecurityIdentifier { get; init; }
        public string? OnPremisesUserPrincipalName { get; init; }
        public string? OnPremisesDomainName { get; init; }
    }

    private sealed record GraphManagerDto
    {
        public string Id { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
    }

    private sealed record GraphSponsorDto
    {
        public string Id { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
    }

    private sealed record GraphGroup
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; init; }
        public string? Mail { get; init; }
        public bool? MailEnabled { get; init; }
        public bool? SecurityEnabled { get; init; }
        public string? MailNickname { get; init; }
        public List<string>? GroupTypes { get; init; }
        public string? Visibility { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
        public bool? OnPremisesSyncEnabled { get; init; }
    }

    private sealed record MemberDto(
        [property: JsonPropertyName("@odata.type")] string? OdataType,
        string? Id);

    private sealed record MemberOfDto(string? Id);

    private sealed record OwnerDto(string? Id);

    private sealed record TransitiveMemberDto(string? Id);

    private sealed record GraphServicePrincipalDto
    {
        public string? Id { get; init; }
        public string? AppId { get; init; }
        public string? DisplayName { get; init; }
        public string? AppDisplayName { get; init; }
        public string? ServicePrincipalType { get; init; }
        public bool? AccountEnabled { get; init; }
        public string? PublisherName { get; init; }
        public string? SignInAudience { get; init; }
        public List<string>? Tags { get; init; }
        public string? AppOwnerOrganizationId { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
    }

    private sealed record GraphApplicationDto
    {
        public string? Id { get; init; }
        public string? AppId { get; init; }
        public string? DisplayName { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
        public string? SignInAudience { get; init; }
        public string? PublisherDomain { get; init; }
        public List<string>? IdentifierUris { get; init; }
        public List<string>? Tags { get; init; }
        public GraphVerifiedPublisherDto? VerifiedPublisher { get; init; }
        public GraphCertificationDto? Certification { get; init; }

        // New properties
        public DateTimeOffset? DeletedDateTime { get; init; }
        public bool? IsFallbackPublicClient { get; init; }
        public string? ApplicationTemplateId { get; init; }
        public string? CreatedByAppId { get; init; }
        public string? DisabledByMicrosoftStatus { get; init; }
        public bool? IsDeviceOnlyAuthSupported { get; init; }
        public string? GroupMembershipClaims { get; init; }
        public object? OptionalClaims { get; init; }
        public List<object>? AddIns { get; init; }
        public string? SamlMetadataUrl { get; init; }
        public string? TokenEncryptionKeyId { get; init; }
        public GraphApiApplicationDto? Api { get; init; }
        public List<object>? AppRoles { get; init; }
        public GraphPublicClientApplicationDto? PublicClient { get; init; }
        public GraphInformationalUrlDto? Info { get; init; }
        public List<object>? KeyCredentials { get; init; }
        public GraphParentalControlSettingsDto? ParentalControlSettings { get; init; }
        public List<object>? PasswordCredentials { get; init; }
        public List<object>? RequiredResourceAccess { get; init; }
        public GraphWebApplicationDto? Web { get; init; }
    }

    private sealed record GraphVerifiedPublisherDto
    {
        public string? DisplayName { get; init; }
        public string? VerifiedPublisherId { get; init; }
        public DateTimeOffset? AddedDateTime { get; init; }
    }

    private sealed record GraphCertificationDto
    {
        public bool? IsPublisherAttested { get; init; }
        public bool? IsCertifiedByMicrosoft { get; init; }
        public DateTimeOffset? LastCertificationDateTime { get; init; }
        public DateTimeOffset? CertificationExpirationDateTime { get; init; }
        public string? CertificationDetailsUrl { get; init; }
    }

    private sealed record GraphApiApplicationDto
    {
        public int? RequestedAccessTokenVersion { get; init; }
        public bool? AcceptMappedClaims { get; init; }
        public List<object>? KnownClientApplications { get; init; }
        public List<object>? Oauth2PermissionScopes { get; init; }
        public List<object>? PreAuthorizedApplications { get; init; }
    }

    private sealed record GraphPublicClientApplicationDto
    {
        public List<string>? RedirectUris { get; init; }
    }

    private sealed record GraphInformationalUrlDto
    {
        public string? TermsOfServiceUrl { get; init; }
        public string? SupportUrl { get; init; }
        public string? PrivacyStatementUrl { get; init; }
        public string? MarketingUrl { get; init; }
        public string? LogoUrl { get; init; }
    }

    private sealed record GraphParentalControlSettingsDto
    {
        public List<string>? CountriesBlockedForMinors { get; init; }
        public string? LegalAgeGroupRule { get; init; }
    }

    private sealed record GraphWebApplicationDto
    {
        public List<string>? RedirectUris { get; init; }
        public string? HomePageUrl { get; init; }
        public string? LogoutUrl { get; init; }
        public GraphImplicitGrantSettingsDto? ImplicitGrantSettings { get; init; }
    }

    private sealed record GraphImplicitGrantSettingsDto
    {
        public bool? EnableIdTokenIssuance { get; init; }
        public bool? EnableAccessTokenIssuance { get; init; }
    }

    private sealed record GraphCollectionWrapper<T>
    {
        [JsonPropertyName("value")]
        public List<T>? Value { get; init; }

        [JsonPropertyName("@odata.nextLink")]
        public string? OdataNextLink { get; init; }

        [JsonPropertyName("@odata.count")]
        public int? OdataCount { get; init; }
    }
}

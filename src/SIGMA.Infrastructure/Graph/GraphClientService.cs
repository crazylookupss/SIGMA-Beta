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
        // Apply enterprise application filter to match Microsoft Entra Admin Center exactly
        // Microsoft Entra ID uses the tag 'WindowsAzureActiveDirectoryIntegratedApp' to identify
        // service principals that appear in the "Enterprise applications" blade.
        var enterpriseAppFilter = "tags/Any(x: x eq 'WindowsAzureActiveDirectoryIntegratedApp')";
        var combinedFilter = string.IsNullOrWhiteSpace(filter)
            ? enterpriseAppFilter
            : $"({enterpriseAppFilter}) and ({filter})";

        var result = await GetPagedAsync<GraphServicePrincipalDto, EntraServicePrincipal>(
            "servicePrincipals", MapServicePrincipal, select, combinedFilter, top, skip, count, cancellationToken);

        if (result.IsFailure)
            return result;

        // Enrichment: fetch user assignments and credential health for all enterprise apps
        var enrichedData = await Task.WhenAll(
            result.Value!.Data.Select(sp => EnrichServicePrincipalAsync(sp, cancellationToken)));

        return Result.Success(new PagedResponse<EntraServicePrincipal>
        {
            Data = enrichedData.ToList(),
            NextLink = result.Value.NextLink,
            Count = result.Value.Count
        });
    }

    public async Task<Result<EntraServicePrincipal>> GetServicePrincipalByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(select))
        {
            select = "id,appId,displayName,appDisplayName,servicePrincipalType,accountEnabled,publisherName,signInAudience,tags,appOwnerOrganizationId,createdDateTime,appRoleAssignmentRequired,preferredSingleSignOnMode,description,notificationEmailAddresses,appRoles,keyCredentials,passwordCredentials";
        }

        var result = await GetSingleAsync<GraphServicePrincipalDto, EntraServicePrincipal>(
            $"servicePrincipals/{Uri.EscapeDataString(id)}", MapServicePrincipal, select, cancellationToken);

        if (result.IsFailure)
            return result.Error!;

        var enriched = await EnrichServicePrincipalAsync(result.Value!, cancellationToken);
        return Result.Success(enriched);
    }

    private async Task<EntraServicePrincipal> EnrichServicePrincipalAsync(EntraServicePrincipal sp, CancellationToken ct)
    {
        var usersCount = 0;
        try
        {
            var url = $"servicePrincipals/{Uri.EscapeDataString(sp.Id)}/appRoleAssignedTo?$count=true&$top=1";
            var response = await SendGetAsync(url, ct, eventualConsistency: true);
            if (response.IsSuccessStatusCode)
            {
                var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<object>>(JsonOptions, ct);
                usersCount = wrapper?.OdataCount ?? 0;
            }
        }
        catch { }

        var hasExpiringKeys = false;
        try
        {
            var url = $"servicePrincipals/{Uri.EscapeDataString(sp.Id)}?$select=keyCredentials,passwordCredentials";
            var response = await SendGetAsync(url, ct);
            if (response.IsSuccessStatusCode)
            {
                var creds = await response.Content.ReadFromJsonAsync<GraphServicePrincipalCredentialsDto>(JsonOptions, ct);
                var now = DateTimeOffset.UtcNow;
                if (creds?.KeyCredentials != null)
                {
                    foreach (var key in creds.KeyCredentials)
                    {
                        if (key.EndDateTime.HasValue && key.EndDateTime.Value < now.AddDays(30))
                        {
                            hasExpiringKeys = true;
                            break;
                        }
                    }
                }
                if (!hasExpiringKeys && creds?.PasswordCredentials != null)
                {
                    foreach (var pw in creds.PasswordCredentials)
                    {
                        if (pw.EndDateTime.HasValue && pw.EndDateTime.Value < now.AddDays(30))
                        {
                            hasExpiringKeys = true;
                            break;
                        }
                    }
                }
            }
        }
        catch { }

        var status = "Active";
        if (sp.AccountEnabled == false)
        {
            status = "Error";
        }
        else if (hasExpiringKeys)
        {
            status = "Warning";
        }

        var lastSignIn = (DateTimeOffset?)null;

        return sp with
        {
            UsersCount = usersCount,
            AssignedUserCount = usersCount,
            AssignedGroupCount = 0,
            SignInStatus = status,
            LastSignIn = lastSignIn
        };

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

    public async Task<List<SignInHistoryEntry>> GetSignInHistoryAsync(int days = 30, CancellationToken ct = default)
    {
        try
        {
            var url = $"auditLogs/signIns?$top={days}&$orderby=createdDateTime desc&$select=createdDateTime&$count=true";
            var response = await SendGetAsync(url, ct, eventualConsistency: true);

            if (!response.IsSuccessStatusCode)
                return [];

            var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphSignInEntry>>(JsonOptions, ct);
            if (wrapper?.Value == null || wrapper.Value.Count == 0)
                return [];

            return wrapper.Value.Select(s => new SignInHistoryEntry(s.CreatedDateTime)).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<EntraAppAssignment>> GetServicePrincipalAssignmentsAsync(
        string servicePrincipalId, CancellationToken ct = default)
    {
        try
        {
            var url = $"servicePrincipals/{Uri.EscapeDataString(servicePrincipalId)}/appRoleAssignedTo";
            var wrapper = await GetCollectionListAsync<GraphAppRoleAssignment>(url, ct);
            return wrapper.Select(MapAssignment).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<EntraAppOwner>> GetServicePrincipalOwnersAsync(
        string servicePrincipalId, CancellationToken ct = default)
    {
        try
        {
            var url = $"servicePrincipals/{Uri.EscapeDataString(servicePrincipalId)}/owners?$select=id,displayName,userPrincipalName";
            var wrapper = await GetCollectionListAsync<GraphOwnerEntry>(url, ct);
            return wrapper.Select(MapOwner).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<SignInHistoryEntry>> GetSignInsForServicePrincipalAsync(
        string appId, int days = 7, CancellationToken ct = default)
    {
        try
        {
            var filter = $"appId eq '{Uri.EscapeDataString(appId)}'";
            var url = $"auditLogs/signIns?$filter={Uri.EscapeDataString(filter)}&$top=100&$orderby=createdDateTime desc&$select=createdDateTime";
            var response = await SendGetAsync(url, ct, eventualConsistency: true);

            if (!response.IsSuccessStatusCode)
                return [];

            var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphSignInEntry>>(JsonOptions, ct);
            if (wrapper?.Value == null)
                return [];

            return wrapper.Value.Select(s => new SignInHistoryEntry(s.CreatedDateTime)).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<Result<EntraApplicationDetails>> GetApplicationByAppIdAsync(
        string appId, CancellationToken ct = default)
    {
        try
        {
            var filter = $"appId eq '{Uri.EscapeDataString(appId)}'";
            var select = "id,appId,displayName,signInAudience,publisherDomain,identifierUris,tags,createdDateTime,applicationTemplateId,"
                       + "web,api,requiredResourceAccess,appRoles,keyCredentials,passwordCredentials,info,"
                       + "verifiedPublisher,certification,samlMetadataUrl,tokenEncryptionKeyId,isFallbackPublicClient,publicClient";
            var url = BuildUrl("applications", select, filter, null, null, null);
            var response = await SendGetAsync(url, ct, eventualConsistency: true);

            if (!response.IsSuccessStatusCode)
                return await HandleErrorResponse(response);

            var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphApplicationDto>>(JsonOptions, ct);
            var app = wrapper?.Value?.FirstOrDefault();
            if (app == null)
                return Error.NotFound("Application.NotFound", $"No application registration found with appId '{appId}'.");

            var appDetails = MapApplicationDetails(app);
            await EnrichPermissionsAsync(appDetails.RequiredResourceAccess, ct);
            return Result.Success(appDetails);
        }
        catch (HttpRequestException ex)
        {
            return Error.ExternalService("GraphError", ex.Message);
        }
        catch (Exception ex)
        {
            return Error.ExternalService("GraphUnexpected", $"Unexpected error: {ex.Message}");
        }
    }

    private async Task EnrichPermissionsAsync(List<EntraAppPermission> permissions, CancellationToken ct)
    {
        if (permissions == null || permissions.Count == 0)
            return;

        var uniqueResourceAppIds = permissions
            .Select(p => p.ResourceAppId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        if (uniqueResourceAppIds.Count == 0)
            return;

        // Construct filter: appId eq 'guid1' or appId eq 'guid2'
        var filterParts = uniqueResourceAppIds.Select(id => $"appId eq '{Uri.EscapeDataString(id)}'");
        var filter = string.Join(" or ", filterParts);
        var select = "appId,displayName,oauth2PermissionScopes,appRoles";
        var url = BuildUrl("servicePrincipals", select, filter, null, null, null);

        try
        {
            var response = await SendGetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return;

            var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphServicePrincipalResolveDto>>(JsonOptions, ct);
            if (wrapper?.Value == null || wrapper.Value.Count == 0)
                return;

            var resourceNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var delegatedPermissionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var applicationPermissionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sp in wrapper.Value)
            {
                if (string.IsNullOrEmpty(sp.AppId)) continue;

                if (!string.IsNullOrEmpty(sp.DisplayName))
                {
                    resourceNameMap[sp.AppId] = sp.DisplayName;
                }

                if (sp.Oauth2PermissionScopes != null)
                {
                    foreach (var scope in sp.Oauth2PermissionScopes)
                    {
                        if (string.IsNullOrEmpty(scope.Id) || string.IsNullOrEmpty(scope.Value)) continue;
                        delegatedPermissionMap[$"{sp.AppId}:{scope.Id}"] = scope.Value;
                    }
                }

                if (sp.AppRoles != null)
                {
                    foreach (var role in sp.AppRoles)
                    {
                        if (string.IsNullOrEmpty(role.Id) || string.IsNullOrEmpty(role.Value)) continue;
                        applicationPermissionMap[$"{sp.AppId}:{role.Id}"] = role.Value;
                    }
                }
            }

            for (int i = 0; i < permissions.Count; i++)
            {
                var p = permissions[i];
                var appId = p.ResourceAppId;
                var resolvedName = resourceNameMap.TryGetValue(appId, out var name) ? name : appId;

                var resolvedDelegated = p.DelegatedPermissions
                    .Select(guid => delegatedPermissionMap.TryGetValue($"{appId}:{guid}", out var val) ? val : guid)
                    .ToList();

                var resolvedApplication = p.ApplicationPermissions
                    .Select(guid => applicationPermissionMap.TryGetValue($"{appId}:{guid}", out var val) ? val : guid)
                    .ToList();

                permissions[i] = p with
                {
                    ResourceDisplayName = resolvedName,
                    DelegatedPermissions = resolvedDelegated,
                    ApplicationPermissions = resolvedApplication
                };
            }
        }
        catch
        {
            // Fail gracefully - leave permissions as raw GUIDs
        }
    }

    private static EntraAppAssignment MapAssignment(GraphAppRoleAssignment a) => new()
    {
        Id = a.Id ?? string.Empty,
        PrincipalId = a.PrincipalId ?? string.Empty,
        PrincipalDisplayName = a.PrincipalDisplayName ?? string.Empty,
        PrincipalType = a.PrincipalType ?? "User",
        AppRoleId = a.AppRoleId,
        AppRoleValue = a.AppRoleValue,
        CreatedDateTime = a.CreatedDateTime,
    };

    private static EntraAppOwner MapOwner(GraphOwnerEntry o) => new()
    {
        Id = o.Id ?? string.Empty,
        DisplayName = o.DisplayName ?? string.Empty,
        UserPrincipalName = o.UserPrincipalName,
        OwnerType = o.OdataType?.Contains("user", StringComparison.OrdinalIgnoreCase) == true ? "User" : "ServicePrincipal",
    };

    private static EntraApplicationDetails MapApplicationDetails(GraphApplicationDto app)
    {
        var redirectUris = new List<string>();
        var logoutUrls = new List<string>();
        string? homePageUrl = null;
        bool? enableIdToken = null;
        bool? enableAccessToken = null;

        if (app.Web != null)
        {
            if (app.Web.RedirectUris != null)
                redirectUris.AddRange(app.Web.RedirectUris);
            homePageUrl = app.Web.HomePageUrl;
            if (app.Web.LogoutUrl != null)
                logoutUrls.Add(app.Web.LogoutUrl);
            if (app.Web.ImplicitGrantSettings != null)
            {
                enableIdToken = app.Web.ImplicitGrantSettings.EnableIdTokenIssuance;
                enableAccessToken = app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance;
            }
        }

        var publicClientRedirects = app.PublicClient?.RedirectUris ?? [];
        if (publicClientRedirects.Count > 0)
            redirectUris.AddRange(publicClientRedirects);

        var apiScopes = new List<string>();
        if (app.Api?.Oauth2PermissionScopes != null)
        {
            foreach (var scope in app.Api.Oauth2PermissionScopes)
            {
                if (scope is Dictionary<string, object> scopeDict &&
                    scopeDict.TryGetValue("value", out var scopeVal) &&
                    scopeVal is string scopeStr)
                {
                    apiScopes.Add(scopeStr);
                }
            }
        }

        var requiredAccess = new List<EntraAppPermission>();
        if (app.RequiredResourceAccess != null)
        {
            foreach (var rra in app.RequiredResourceAccess)
            {
                if (rra.ResourceAccess == null) continue;
                var delegated = new List<string>();
                var application = new List<string>();
                foreach (var ra in rra.ResourceAccess)
                {
                    if (ra.Type == "Scope") delegated.Add(ra.Id ?? string.Empty);
                    else if (ra.Type == "Role") application.Add(ra.Id ?? string.Empty);
                }
                requiredAccess.Add(new EntraAppPermission
                {
                    ResourceAppId = rra.ResourceAppId ?? string.Empty,
                    DelegatedPermissions = delegated,
                    ApplicationPermissions = application,
                });
            }
        }

        return new EntraApplicationDetails
        {
            Id = app.Id ?? string.Empty,
            AppId = app.AppId ?? string.Empty,
            DisplayName = app.DisplayName,
            SignInAudience = app.SignInAudience,
            PublisherDomain = app.PublisherDomain,
            IdentifierUris = app.IdentifierUris ?? [],
            Tags = app.Tags ?? [],
            RedirectUris = redirectUris,
            LogoutUrls = logoutUrls,
            HomePageUrl = homePageUrl,
            RequiredResourceAccess = requiredAccess,
            AppRoles = [],
            KeyCredentials = (app.KeyCredentials ?? []).Select(k => new EntraKeyCredential
            {
                KeyId = k.KeyId,
                DisplayName = k.DisplayName,
                Type = k.Type,
                Usage = k.Usage,
                StartDateTime = k.StartDateTime,
                EndDateTime = k.EndDateTime,
                Thumbprint = DecodeThumbprint(k.CustomKeyIdentifier),
            }).ToList(),
            PasswordCredentials = (app.PasswordCredentials ?? []).Select(p => new EntraPasswordCredential
            {
                KeyId = p.KeyId,
                DisplayName = p.DisplayName,
                StartDateTime = p.StartDateTime,
                EndDateTime = p.EndDateTime,
                Hint = p.Hint,
            }).ToList(),
            SamlMetadataUrl = app.SamlMetadataUrl,
            TokenEncryptionKeyId = app.TokenEncryptionKeyId,
            ApplicationTemplateId = app.ApplicationTemplateId,
            CreatedDateTime = app.CreatedDateTime,
            LogoUrl = app.Info?.LogoUrl,
            TermsOfServiceUrl = app.Info?.TermsOfServiceUrl,
            PrivacyStatementUrl = app.Info?.PrivacyStatementUrl,
            SupportUrl = app.Info?.SupportUrl,
            VerifiedPublisher = app.VerifiedPublisher is null ? null : new VerifiedPublisherInfo
            {
                DisplayName = app.VerifiedPublisher.DisplayName,
                VerifiedPublisherId = app.VerifiedPublisher.VerifiedPublisherId,
                AddedDateTime = app.VerifiedPublisher.AddedDateTime,
            },
            Certification = app.Certification is null ? null : new CertificationInfo
            {
                IsPublisherAttested = app.Certification.IsPublisherAttested,
                IsCertifiedByMicrosoft = app.Certification.IsCertifiedByMicrosoft,
                LastCertificationDateTime = app.Certification.LastCertificationDateTime,
                CertificationExpirationDateTime = app.Certification.CertificationExpirationDateTime,
                CertificationDetailsUrl = app.Certification.CertificationDetailsUrl,
            },
            IsFallbackPublicClient = app.IsFallbackPublicClient,
            EnableIdTokenIssuance = enableIdToken,
            EnableAccessTokenIssuance = enableAccessToken,
            RequestedAccessTokenVersion = app.Api?.RequestedAccessTokenVersion,
            AcceptMappedClaims = app.Api?.AcceptMappedClaims,
            Oauth2PermissionScopeValues = apiScopes,
            PublicClientRedirectUris = publicClientRedirects,
        };
    }

    public async Task<Result<EntraTenant>> GetTenantDetailsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var orgResponse = await SendGetAsync("organization", cancellationToken);
            if (!orgResponse.IsSuccessStatusCode)
                return await HandleErrorResponse(orgResponse);

            var orgWrapper = await orgResponse.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphOrganizationDto>>(JsonOptions, cancellationToken);
            var org = orgWrapper?.Value?.FirstOrDefault();
            if (org == null)
                return Error.NotFound("Organization.NotFound", "No organization metadata found.");

            // Fetch directory counts in parallel for optimal load times
            var usersTask = GetCountAsync("users", eventualConsistency: true, cancellationToken);
            var groupsTask = GetCountAsync("groups", eventualConsistency: true, cancellationToken);
            var appsTask = GetCountAsync("applications", eventualConsistency: true, cancellationToken);
            var spTask = GetCountAsync("servicePrincipals", eventualConsistency: true, cancellationToken, "tags/Any(x: x eq 'WindowsAzureActiveDirectoryIntegratedApp')");
            var devicesTask = GetCountAsync("devices", eventualConsistency: true, cancellationToken);



            await Task.WhenAll(usersTask, groupsTask, appsTask, spTask, devicesTask);

            var license = "Microsoft Entra ID Free";
            try
            {
                var skuResponse = await SendGetAsync("subscribedSkus", cancellationToken);
                if (skuResponse.IsSuccessStatusCode)
                {
                    var skuWrapper = await skuResponse.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphSkuDto>>(JsonOptions, cancellationToken);
                    var skus = skuWrapper?.Value ?? [];
                    if (skus.Any(s => s.SkuPartNumber == "AAD_PREMIUM_P2"))
                        license = "Microsoft Entra ID P2";
                    else if (skus.Any(s => s.SkuPartNumber == "AAD_PREMIUM"))
                        license = "Microsoft Entra ID P1";
                }
            }
            catch 
            {
                // Fall back gracefully to Microsoft Entra ID Free if permissions to read SKUs are restricted
            }

            var primaryDomain = org.VerifiedDomains?.FirstOrDefault(d => d.IsDefault == true)?.Name ?? "unknown";

            return Result.Success(new EntraTenant
            {
                Id = org.Id ?? string.Empty,
                DisplayName = org.DisplayName ?? "Default Directory",
                PrimaryDomain = primaryDomain,
                License = license,
                UsersCount = usersTask.Result,
                GroupsCount = groupsTask.Result,
                ApplicationsCount = appsTask.Result,
                EnterpriseApplicationsCount = spTask.Result,
                DevicesCount = devicesTask.Result
            });
        }
        catch (Exception ex)
        {
            return Error.ExternalService("TenantError", ex.Message);
        }
    }

    private async Task<int> GetCountAsync(string path, bool eventualConsistency, CancellationToken ct, string? filter = null)
    {
        try
        {
            var url = $"{path}?$top=1&$count=true";
            if (!string.IsNullOrEmpty(filter))
            {
                url += $"&$filter={Uri.EscapeDataString(filter)}";
            }
            var response = await SendGetAsync(url, ct, eventualConsistency);
            if (!response.IsSuccessStatusCode)
                return 0;

            var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<object>>(JsonOptions, ct);
            return wrapper?.OdataCount ?? 0;
        }
        catch
        {
            return 0;
        }
    }



    private async Task<Result<PagedResponse<TTarget>>> GetPagedAsync<TSource, TTarget>(
        string path, Func<TSource, TTarget> mapper,
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken ct)
    {
        try
        {
            var isManualPagination = top.HasValue;
            var allItems = new List<TTarget>();
            int? totalCount = null;
            var url = BuildUrl(path, select, filter, top, skip, count);
            var isFirstPage = true;
            var consistencyNeeded = count == true || filter != null;

            do
            {
                var response = await SendGetAsync(url, ct, eventualConsistency: consistencyNeeded);

                if (!response.IsSuccessStatusCode)
                    return await HandleErrorResponse(response);

                var wrapper = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<TSource>>(JsonOptions, ct);

                if (wrapper?.Value == null || wrapper.Value.Count == 0)
                    break;

                allItems.AddRange(wrapper.Value.Select(mapper));

                if (isFirstPage)
                {
                    totalCount = wrapper.OdataCount;
                    isFirstPage = false;
                }

                url = wrapper.OdataNextLink;

                if (isManualPagination)
                    break;

            } while (url != null);

            if (allItems.Count == 0)
                return Error.NotFound($"{typeof(TSource).Name}.NotFound", "No results found.");

            return Result.Success(new PagedResponse<TTarget>
            {
                Data = allItems,
                NextLink = isManualPagination ? url : null,
                Count = totalCount ?? allItems.Count,
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

    private async Task<HttpResponseMessage> SendGetAsync(string url, CancellationToken ct, bool eventualConsistency = false)
    {
        var token = await GetTokenAsync(ct);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        if (eventualConsistency)
        {
            request.Headers.Add("ConsistencyLevel", "eventual");
        }

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
        AppRoleAssignmentRequired = sp.AppRoleAssignmentRequired,
        PreferredSingleSignOnMode = sp.PreferredSingleSignOnMode,
        AppDescription = sp.AppDescription,
        NotificationEmailAddresses = sp.NotificationEmailAddresses ?? [],
        AppRoles = sp.AppRoles ?? [],
        KeyCredentials = sp.KeyCredentials ?? [],
        PasswordCredentials = sp.PasswordCredentials ?? [],
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
        KeyCredentials = app.KeyCredentials?.Select(k => (object)k).ToList() ?? [],
        ParentalControlSettings = app.ParentalControlSettings is null ? null : new ParentalControlSettingsDto(
            app.ParentalControlSettings.CountriesBlockedForMinors ?? [],
            app.ParentalControlSettings.LegalAgeGroupRule),
        PasswordCredentials = app.PasswordCredentials?.Select(p => (object)p).ToList() ?? [],
        RequiredResourceAccess = app.RequiredResourceAccess?.Select(r => (object)r).ToList() ?? [],
        Web = app.Web is null ? null : new WebApplicationDto(
            app.Web.RedirectUris ?? [],
            app.Web.HomePageUrl,
            app.Web.LogoutUrl,
            app.Web.ImplicitGrantSettings is null ? null : new ImplicitGrantSettingsDto(
                app.Web.ImplicitGrantSettings.EnableIdTokenIssuance,
                app.Web.ImplicitGrantSettings.EnableAccessTokenIssuance))
    };

    public async Task<List<EntraAppOwner>> GetApplicationOwnersAsync(
        string applicationId, CancellationToken ct = default)
    {
        try
        {
            var url = $"applications/{Uri.EscapeDataString(applicationId)}/owners?$select=id,displayName,userPrincipalName";
            var wrapper = await GetCollectionListAsync<GraphOwnerEntry>(url, ct);
            return wrapper.Select(MapOwner).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<ServicePrincipalRef>> GetServicePrincipalsForApplicationAsync(
        string appId, CancellationToken ct = default)
    {
        try
        {
            var filter = $"appId eq '{Uri.EscapeDataString(appId)}'";
            var select = "id,appId,displayName,accountEnabled,createdDateTime";
            var url = BuildUrl("servicePrincipals", select, filter, null, null, null);
            var wrapper = await GetCollectionListAsync<GraphServicePrincipalDto>(url, ct);
            return wrapper.Select(sp => new ServicePrincipalRef
            {
                Id = sp.Id ?? string.Empty,
                AppId = sp.AppId ?? string.Empty,
                DisplayName = sp.DisplayName,
                AccountEnabled = sp.AccountEnabled,
                CreatedDateTime = sp.CreatedDateTime,
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<ApplicationStatistics> GetApplicationStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            // Fetch counts in parallel
            var totalAppsTask = GetCountAsync("applications", eventualConsistency: true, ct);
            var totalSpTask = GetCountAsync("servicePrincipals", eventualConsistency: true, ct,
                "tags/Any(x: x eq 'WindowsAzureActiveDirectoryIntegratedApp')");

            // Fetch app samples to compute status/protocol distribution
            var appsSelect = "id,appId,displayName,signInAudience,publisherDomain,identifierUris,createdDateTime,web,publicClient,api,samlMetadataUrl,keyCredentials,passwordCredentials,verifiedPublisher,certification";
            var appsResult = await GetPagedAsync<GraphApplicationDto, EntraApplication>(
                "applications", MapApplication, appsSelect, null, null, null, null, ct);

            await Task.WhenAll(totalAppsTask, totalSpTask);

            var totalApps = totalAppsTask.Result;
            var totalSp = totalSpTask.Result;

            if (appsResult.IsFailure || appsResult.Value == null)
            {
                return new ApplicationStatistics
                {
                    TotalAppRegistrations = totalApps,
                    TotalServicePrincipals = totalSp,
                };
            }

            var apps = appsResult.Value.Data;
            var now = DateTimeOffset.UtcNow;
            var active = 0;
            var warning = 0;
            var highRisk = 0;
            var multiTenant = 0;
            var externalPublisher = 0;
            var ownerless = 0;
            var expiringSecrets = 0;
            var expiredSecrets = 0;

            var protocolCounts = new Dictionary<string, int>
            {
                ["OpenID Connect"] = 0,
                ["OAuth 2.0"] = 0,
                ["SAML"] = 0,
                ["Client Credentials"] = 0,
                ["Unknown"] = 0,
            };

            foreach (var app in apps)
            {
                var isActive = (app.IdentifierUris?.Count > 0)
                    || (app.Web?.RedirectUris?.Count > 0)
                    || (app.PublicClient?.RedirectUris?.Count > 0);
                var hasRedirectUris = (app.Web?.RedirectUris?.Count ?? 0) > 0
                    || (app.PublicClient?.RedirectUris?.Count ?? 0) > 0;

                // Protocol detection
                var protocol = "Unknown";
                var isSaml = !string.IsNullOrEmpty(app.SamlMetadataUrl) || 
                             (app.Tags != null && (app.Tags.Contains("WindowsAzureActiveDirectoryCustomSingleSignOnApplication") || 
                                                   app.Tags.Contains("WindowsAzureActiveDirectoryGalleryApplicationNonPrimaryV1")));

                if (isSaml)
                {
                    protocol = "SAML";
                }
                else if (hasRedirectUris)
                {
                    var httpUris = (app.Web?.RedirectUris ?? [])
                        .Concat(app.PublicClient?.RedirectUris ?? [])
                        .Any(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase));
                    protocol = httpUris ? "OpenID Connect" : "OAuth 2.0";
                }
                else if (app.Api?.RequestedAccessTokenVersion != null && !hasRedirectUris)
                {
                    protocol = "Client Credentials";
                }

                if (protocolCounts.ContainsKey(protocol))
                    protocolCounts[protocol]++;

                // Credential evaluation
                var hasExpired = false;
                var hasExpiring = false;
                foreach (var k in app.KeyCredentials ?? [])
                {
                    if (k is GraphKeyCredential keyCred)
                    {
                        if (keyCred.EndDateTime.HasValue && keyCred.EndDateTime.Value < now)
                            hasExpired = true;
                        else if (keyCred.EndDateTime.HasValue && keyCred.EndDateTime.Value < now.AddDays(30))
                            hasExpiring = true;
                    }
                }
                foreach (var p in app.PasswordCredentials ?? [])
                {
                    if (p is GraphPasswordCredential pwCred)
                    {
                        if (pwCred.EndDateTime.HasValue && pwCred.EndDateTime.Value < now)
                            hasExpired = true;
                        else if (pwCred.EndDateTime.HasValue && pwCred.EndDateTime.Value < now.AddDays(30))
                            hasExpiring = true;
                    }
                }

                // Risk assessment
                var isMultiTenant = app.SignInAudience == "AzureADMultipleOrgs"
                    || app.SignInAudience == "PersonalMicrosoftAccount";
                var isExternalPub = app.VerifiedPublisher == null
                    && app.PublisherDomain != null
                    && !app.PublisherDomain.Contains("microsoft", StringComparison.OrdinalIgnoreCase)
                    && !app.PublisherDomain.Contains("onmicrosoft", StringComparison.OrdinalIgnoreCase);

                if (isActive)
                    active++;
                if (hasExpiring && !hasExpired)
                    warning++;
                if (hasExpired || isExternalPub)
                    highRisk++;
                if (isMultiTenant)
                    multiTenant++;
                if (isExternalPub)
                    externalPublisher++;
                if (hasExpiring)
                    expiringSecrets++;
                if (hasExpired)
                    expiredSecrets++;
            }

            return new ApplicationStatistics
            {
                TotalAppRegistrations = totalApps,
                ActiveApplications = active,
                WarningApplications = warning,
                HighRiskApplications = highRisk,
                TotalServicePrincipals = totalSp,
                StatusDistribution =
                [
                    new StatusStat { Label = "Active", Count = active },
                    new StatusStat { Label = "Warning", Count = warning },
                    new StatusStat { Label = "High Risk", Count = highRisk },
                ],
                ProtocolDistribution = protocolCounts
                    .Where(p => p.Value > 0)
                    .Select(p => new ProtocolStat { Protocol = p.Key, Count = p.Value })
                    .ToList(),
                MultiTenantApps = multiTenant,
                ExternalPublisherApps = externalPublisher,
                OwnerlessApps = ownerless,
                AppsWithExpiringSecrets = expiringSecrets,
                AppsWithExpiredSecrets = expiredSecrets,
            };
        }
        catch
        {
            return new ApplicationStatistics();
        }
    }

    public async Task<AppCredentialHealth> GetApplicationCredentialsAsync(
        string applicationId, CancellationToken ct = default)
    {
        try
        {
            var select = "id,appId,displayName,keyCredentials,passwordCredentials";
            var result = await GetSingleAsync<GraphApplicationDto, EntraApplication>(
                $"applications/{Uri.EscapeDataString(applicationId)}", MapApplication, select, ct);

            if (result.IsFailure || result.Value == null)
                return new AppCredentialHealth();

            // The keyCredentials/passwordCredentials on EntraApplication are List<object>
            // We need to cast them back to Graph* types. However, MapApplication transforms them
            // to (object)k. For credential health we need the raw data.
            // Fetch directly from Graph for credential evaluation.
            var url = $"applications/{Uri.EscapeDataString(applicationId)}?$select=id,keyCredentials,passwordCredentials";
            var response = await SendGetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new AppCredentialHealth();

            var creds = await response.Content.ReadFromJsonAsync<GraphApplicationCredentialsDto>(JsonOptions, ct);
            if (creds == null)
                return new AppCredentialHealth();

            var now = DateTimeOffset.UtcNow;
            var certificates = new List<CredentialInfo>();
            var secrets = new List<CredentialInfo>();

            foreach (var k in creds.KeyCredentials ?? [])
            {
                var daysUntil = k.EndDateTime.HasValue ? (int)(k.EndDateTime.Value - now).TotalDays : int.MaxValue;
                certificates.Add(new CredentialInfo
                {
                    KeyId = k.KeyId,
                    DisplayName = k.DisplayName,
                    Type = k.Type,
                    Usage = k.Usage,
                    StartDateTime = k.StartDateTime,
                    EndDateTime = k.EndDateTime,
                    IsExpired = k.EndDateTime.HasValue && k.EndDateTime.Value < now,
                    IsExpiringSoon = k.EndDateTime.HasValue && k.EndDateTime.Value >= now && k.EndDateTime.Value < now.AddDays(30),
                    DaysUntilExpiry = daysUntil,
                });
            }

            foreach (var p in creds.PasswordCredentials ?? [])
            {
                var daysUntil = p.EndDateTime.HasValue ? (int)(p.EndDateTime.Value - now).TotalDays : int.MaxValue;
                secrets.Add(new CredentialInfo
                {
                    KeyId = p.KeyId,
                    DisplayName = p.DisplayName,
                    StartDateTime = p.StartDateTime,
                    EndDateTime = p.EndDateTime,
                    Hint = p.Hint,
                    IsExpired = p.EndDateTime.HasValue && p.EndDateTime.Value < now,
                    IsExpiringSoon = p.EndDateTime.HasValue && p.EndDateTime.Value >= now && p.EndDateTime.Value < now.AddDays(30),
                    DaysUntilExpiry = daysUntil,
                });
            }

            return new AppCredentialHealth
            {
                Certificates = certificates,
                Secrets = secrets,
                HasExpiredCredentials = certificates.Any(c => c.IsExpired) || secrets.Any(s => s.IsExpired),
                HasExpiringCredentials = certificates.Any(c => c.IsExpiringSoon) || secrets.Any(s => s.IsExpiringSoon),
                CredentialsExpiringWithin30Days = certificates.Count(c => c.IsExpiringSoon) + secrets.Count(s => s.IsExpiringSoon),
                CredentialsExpired = certificates.Count(c => c.IsExpired) + secrets.Count(s => s.IsExpired),
            };
        }
        catch
        {
            return new AppCredentialHealth();
        }
    }

    public async Task<List<AuditLogEntry>> GetApplicationAuditLogsAsync(
        string appId, int top = 50, CancellationToken ct = default)
    {
        try
        {
            var filter = $"appId eq '{Uri.EscapeDataString(appId)}'";
            var url = BuildUrl("auditLogs/signIns", "id,createdDateTime,userDisplayName,userPrincipalName,appDisplayName,status,ipAddress,location,deviceDetail,clientAppUsed,riskLevel,correlationId", filter, top, null, null);
            var response = await SendGetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return [];

            var body = await response.Content.ReadFromJsonAsync<GraphAuditLogsResponse>(JsonOptions, ct);
            if (body?.Value == null)
                return [];

            return body.Value.Select(e => new AuditLogEntry
            {
                Id = e.Id ?? string.Empty,
                CreatedDateTime = e.CreatedDateTime,
                UserDisplayName = e.UserDisplayName,
                UserPrincipalName = e.UserPrincipalName,
                AppDisplayName = e.AppDisplayName,
                Status = e.Status?.ErrorCode == 0 ? "Success" : "Failure",
                IpAddress = e.IpAddress,
                Location = e.Location?.City is not null ? $"{e.Location.City}, {e.Location.CountryOrRegion}" : null,
                Device = e.DeviceDetail?.DisplayName,
                ClientAppUsed = e.ClientAppUsed,
                RiskLevel = e.RiskLevel,
                CorrelationId = e.CorrelationId,
            }).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<Result<string>> GetApplicationManifestAsync(
        string applicationId, CancellationToken ct = default)
    {
        var escaped = Uri.EscapeDataString(applicationId);
        var url = $"applications/{escaped}?$select=id,displayName,publisherDomain,description,notes";
        var response = await SendGetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return Error.NotFound("Application.NotFound", $"Application '{applicationId}' not found.");
        var json = await response.Content.ReadAsStringAsync(ct);
        return Result.Success(json);
    }

    public async Task<Result<ServicePrincipalRef>> GetServicePrincipalForApplicationAsync(
        string appId, CancellationToken ct = default)
    {
        var filter = $"appId eq '{Uri.EscapeDataString(appId)}'";
        var url = BuildUrl("servicePrincipals", "id,appId,displayName", filter, 1, null, null);
        var response = await SendGetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return Error.NotFound("ServicePrincipal.NotFound", $"No service principal found for appId '{appId}'.");
        var body = await response.Content.ReadFromJsonAsync<GraphServicePrincipalListResponse>(JsonOptions, ct);
        var sp = body?.Value?.FirstOrDefault();
        if (sp == null)
            return Error.NotFound("ServicePrincipal.NotFound", $"No service principal found for appId '{appId}'.");
        return Result.Success(new ServicePrincipalRef
        {
            Id = sp.Id ?? string.Empty,
            DisplayName = sp.DisplayName,
        });
    }

    public async Task<List<EntraGroupMember>> GetGroupMembersAsync(
        string groupId, CancellationToken ct = default)
    {
        try
        {
            var url = $"groups/{Uri.EscapeDataString(groupId)}/members?$select=id,displayName,userPrincipalName,createdDateTime";
            var wrapper = await GetCollectionListAsync<GraphMemberEntry>(url, ct);
            return wrapper.Select(MapGroupMember).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<EntraGroupOwner>> GetGroupOwnersAsync(
        string groupId, CancellationToken ct = default)
    {
        try
        {
            var url = $"groups/{Uri.EscapeDataString(groupId)}/owners?$select=id,displayName,userPrincipalName";
            var wrapper = await GetCollectionListAsync<GraphOwnerEntry>(url, ct);
            return wrapper.Select(MapGroupOwner).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<EntraGroupApplication>> GetGroupAppRoleAssignmentsAsync(
        string groupId, CancellationToken ct = default)
    {
        try
        {
            var url = $"groups/{Uri.EscapeDataString(groupId)}/appRoleAssignments?$select=id,appRoleId,appRoleValue,resourceDisplayName,resourceId,principalDisplayName,createdDateTime";
            var wrapper = await GetCollectionListAsync<GraphAppRoleAssignment>(url, ct);
            return wrapper.Select(MapGroupApplication).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<EntraGroupDevice>> GetGroupDevicesAsync(
        string groupId, CancellationToken ct = default)
    {
        try
        {
            var url = $"groups/{Uri.EscapeDataString(groupId)}/members/microsoft.graph.device?$select=id,displayName,deviceId,operatingSystem,osVersion,isCompliant,isManaged,trustType";
            var wrapper = await GetCollectionListAsync<GraphDeviceEntry>(url, ct);
            return wrapper.Select(MapGroupDevice).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<EntraGroupAuditLog>> GetGroupAuditLogsAsync(
        string groupId, int top = 50, CancellationToken ct = default)
    {
        try
        {
            var filter = $"targetResources/any(t:t/id eq '{Uri.EscapeDataString(groupId)}')";
            var select = "id,activityDisplayName,category,initiatedBy,result,resultReason,activityDateTime,correlationId,targetResources";
            var url = BuildUrl("auditLogs/directoryAudits", select, filter, top, null, null);
            var response = await SendGetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return [];

            var body = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphDirectoryAuditEntry>>(JsonOptions, ct);
            if (body?.Value == null)
                return [];

            return body.Value.Select(MapGroupAuditLog).ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<EntraGroupAccessReview>> GetGroupAccessReviewsAsync(
        string groupId, CancellationToken ct = default)
    {
        try
        {
            var filter = $"scope/microsoft.graph.accessReviewQueryScope/query eq '/groups/{Uri.EscapeDataString(groupId)}'";
            var select = "id,displayName,status,startDate,endDate,reviewers,instances";
            var url = BuildUrl("identityGovernance/accessReviews/definitions", select, filter, 50, null, null);
            var response = await SendGetAsync(url, ct, eventualConsistency: true);
            if (!response.IsSuccessStatusCode)
                return [];

            var body = await response.Content.ReadFromJsonAsync<GraphCollectionWrapper<GraphAccessReviewDefinition>>(JsonOptions, ct);
            if (body?.Value == null)
                return [];

            return body.Value.Select(MapGroupAccessReview).ToList();
        }
        catch
        {
            return [];
        }
    }

    // Private mapping functions for group details

    private static EntraGroupMember MapGroupMember(GraphMemberEntry m) => new()
    {
        Id = m.Id ?? string.Empty,
        DisplayName = m.DisplayName,
        UserPrincipalName = m.UserPrincipalName,
        MemberType = m.OdataType?.Contains("user", StringComparison.OrdinalIgnoreCase) == true ? "User"
            : m.OdataType?.Contains("group", StringComparison.OrdinalIgnoreCase) == true ? "Group"
            : m.OdataType?.Contains("device", StringComparison.OrdinalIgnoreCase) == true ? "Device"
            : "ServicePrincipal",
        CreatedDateTime = m.CreatedDateTime,
    };

    private static EntraGroupOwner MapGroupOwner(GraphOwnerEntry o) => new()
    {
        Id = o.Id ?? string.Empty,
        DisplayName = o.DisplayName,
        UserPrincipalName = o.UserPrincipalName,
        OwnerType = o.OdataType?.Contains("user", StringComparison.OrdinalIgnoreCase) == true ? "User" : "ServicePrincipal",
    };

    private static EntraGroupApplication MapGroupApplication(GraphAppRoleAssignment a) => new()
    {
        Id = a.Id ?? string.Empty,
        DisplayName = a.PrincipalDisplayName,
        ResourceId = a.ResourceId,
        AppRoleId = a.AppRoleId,
        CreatedDateTime = a.CreatedDateTime,
    };

    private static EntraGroupDevice MapGroupDevice(GraphDeviceEntry d) => new()
    {
        Id = d.Id ?? string.Empty,
        DisplayName = d.DisplayName,
        DeviceId = d.DeviceId,
        OperatingSystem = d.OperatingSystem,
        OsVersion = d.OsVersion,
        IsCompliant = d.IsCompliant,
        IsManaged = d.IsManaged,
        TrustType = d.TrustType,
    };

    private static EntraGroupAuditLog MapGroupAuditLog(GraphDirectoryAuditEntry e) => new()
    {
        Id = e.Id ?? string.Empty,
        ActivityDisplayName = e.ActivityDisplayName,
        Category = e.Category,
        InitiatedBy = e.InitiatedBy?.User?.DisplayName ?? e.InitiatedBy?.App?.DisplayName,
        TargetResourceName = e.TargetResources?.FirstOrDefault()?.DisplayName,
        Result = e.Result,
        ResultReason = e.ResultReason,
        ActivityDateTime = e.ActivityDateTime,
        CorrelationId = e.CorrelationId,
    };

    private static EntraGroupAccessReview MapGroupAccessReview(GraphAccessReviewDefinition d) => new()
    {
        Id = d.Id ?? string.Empty,
        DisplayName = d.DisplayName,
        Status = d.Status,
        StartDate = d.StartDate,
        EndDate = d.EndDate,
        ReviewersCount = d.Reviewers?.Count ?? 0,
    };

    public void Dispose() => _httpClient.Dispose();

    public async Task<Result<ServicePrincipalSsoConfig>> GetServicePrincipalSsoConfigAsync(
        string servicePrincipalId, CancellationToken ct = default)
    {
        var spResult = await GetServicePrincipalByIdAsync(servicePrincipalId, null, ct);
        if (spResult.IsFailure)
            return spResult.Error!;

        var sp = spResult.Value!;
        if (string.IsNullOrEmpty(sp.AppId))
            return Error.NotFound("SsoConfig.NoLinkedApp", "Service principal has no linked application registration.");

        var config = new ServicePrincipalSsoConfig
        {
            PreferredSingleSignOnMode = sp.PreferredSingleSignOnMode ?? string.Empty,
            TenantId = sp.AppOwnerOrganizationId,
        };

        var appResult = await GetApplicationByAppIdAsync(sp.AppId, ct);
        if (appResult.IsSuccess)
        {
            var app = appResult.Value!;
            config.SamlMetadataUrl = app.SamlMetadataUrl;
            config.EntityId = app.IdentifierUris?.FirstOrDefault();
            config.ReplyUrls = app.RedirectUris;
            config.SignOnUrl = app.HomePageUrl;
            config.LogoutUrl = app.LogoutUrls?.FirstOrDefault();
            config.HomePageUrl = app.HomePageUrl;
            config.Certificates = app.KeyCredentials
                .Where(k => k.Type == "AsymmetricX509Cert" || k.Usage == "Verify" || k.Usage == "Sign")
                .Select(k => new SsoCertificate
                {
                    KeyId = k.KeyId,
                    DisplayName = k.DisplayName,
                    Thumbprint = k.Thumbprint,
                    Type = k.Type,
                    Usage = k.Usage,
                    StartDateTime = k.StartDateTime,
                    EndDateTime = k.EndDateTime,
                })
                .ToList();
        }

        var tenantId = sp.AppOwnerOrganizationId ?? sp.Id;
        config.AuthorizationEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize";
        config.TokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        config.Issuer = $"https://sts.windows.net/{tenantId}/";
        config.FederationMetadataUrl = $"https://login.microsoftonline.com/{tenantId}/federationmetadata/2007-06/federationmetadata.xml?appid={sp.AppId}";
        config.LoginUrl = $"https://login.microsoftonline.com/{tenantId}/saml2";
        config.MicrosoftEntraIdentifier = $"https://sts.windows.net/{tenantId}/";

        return Result.Success(config);
    }

    private static string? DecodeThumbprint(string? customKeyIdentifier)
    {
        if (string.IsNullOrEmpty(customKeyIdentifier))
            return null;
        try
        {
            var bytes = Convert.FromBase64String(customKeyIdentifier);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
        catch
        {
            return customKeyIdentifier;
        }
    }

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
        public bool AppRoleAssignmentRequired { get; init; }
        public string? PreferredSingleSignOnMode { get; init; }
        public string? AppDescription { get; init; }
        public List<string>? NotificationEmailAddresses { get; init; }
        public List<object>? AppRoles { get; init; }
        public List<object>? KeyCredentials { get; init; }
        public List<object>? PasswordCredentials { get; init; }
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
        public List<GraphKeyCredential>? KeyCredentials { get; init; }
        public GraphParentalControlSettingsDto? ParentalControlSettings { get; init; }
        public List<GraphPasswordCredential>? PasswordCredentials { get; init; }
        public List<GraphRequiredResourceAccess>? RequiredResourceAccess { get; init; }
        public GraphWebApplicationDto? Web { get; init; }
    }

    private sealed record GraphRequiredResourceAccess
    {
        public string? ResourceAppId { get; init; }
        public List<GraphResourceAccess>? ResourceAccess { get; init; }
    }

    private sealed record GraphResourceAccess
    {
        public string? Id { get; init; }
        public string? Type { get; init; }
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

    private sealed record GraphOrganizationDto
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public List<VerifiedDomainDto>? VerifiedDomains { get; init; }
    }

    private sealed record VerifiedDomainDto
    {
        public string? Name { get; init; }
        public bool? IsDefault { get; init; }
    }

    private sealed record GraphSkuDto
    {
        public string? SkuPartNumber { get; init; }
    }

    private sealed record GraphServicePrincipalCredentialsDto
    {
        public List<GraphKeyCredential>? KeyCredentials { get; init; }
        public List<GraphPasswordCredential>? PasswordCredentials { get; init; }
    }

    private sealed record GraphKeyCredential
    {
        public string? KeyId { get; init; }
        public string? DisplayName { get; init; }
        public string? Type { get; init; }
        public string? Usage { get; init; }
        public DateTimeOffset? StartDateTime { get; init; }
        public DateTimeOffset? EndDateTime { get; init; }
        public string? CustomKeyIdentifier { get; init; }
    }

    private sealed record GraphPasswordCredential
    {
        public string? KeyId { get; init; }
        public string? DisplayName { get; init; }
        public DateTimeOffset? StartDateTime { get; init; }
        public DateTimeOffset? EndDateTime { get; init; }
        public string? Hint { get; init; }
    }

    private sealed record GraphSignInEntry
    {
        public DateTimeOffset? CreatedDateTime { get; init; }
    }

    private sealed record GraphAppRoleAssignment
    {
        public string? Id { get; init; }
        public string? PrincipalId { get; init; }
        public string? PrincipalDisplayName { get; init; }
        public string? PrincipalType { get; init; }
        public string? ResourceId { get; init; }
        public string? AppRoleId { get; init; }
        public string? AppRoleValue { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
    }

    private sealed record GraphOwnerEntry
    {
        [JsonPropertyName("@odata.type")]
        public string? OdataType { get; init; }
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
    }

    private sealed record GraphApplicationCredentialsDto
    {
        public List<GraphKeyCredential>? KeyCredentials { get; init; }
        public List<GraphPasswordCredential>? PasswordCredentials { get; init; }
    }

    private sealed record GraphAuditLogsResponse
    {
        public List<GraphAuditLogEntry>? Value { get; init; }
    }

    private sealed record GraphAuditLogEntry
    {
        public string? Id { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
        public string? UserDisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
        public string? AppDisplayName { get; init; }
        public GraphStatus? Status { get; init; }
        public string? IpAddress { get; init; }
        public GraphSignInLocation? Location { get; init; }
        public GraphDeviceDetail? DeviceDetail { get; init; }
        public string? ClientAppUsed { get; init; }
        public string? RiskLevel { get; init; }
        public string? CorrelationId { get; init; }
    }

    private sealed record GraphStatus
    {
        public int? ErrorCode { get; init; }
    }

    private sealed record GraphSignInLocation
    {
        public string? City { get; init; }
        public string? CountryOrRegion { get; init; }
    }

    private sealed record GraphDeviceDetail
    {
        public string? DisplayName { get; init; }
    }

    private sealed record GraphServicePrincipalListResponse
    {
        public List<GraphServicePrincipalEntry>? Value { get; init; }
    }

    private sealed record GraphServicePrincipalEntry
    {
        public string? Id { get; init; }
        public string? AppId { get; init; }
        public string? DisplayName { get; init; }
    }

    private sealed record GraphServicePrincipalResolveDto
    {
        [JsonPropertyName("appId")] public string? AppId { get; init; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; init; }
        [JsonPropertyName("oauth2PermissionScopes")] public List<GraphScopeResolveDto>? Oauth2PermissionScopes { get; init; }
        [JsonPropertyName("appRoles")] public List<GraphAppRoleResolveDto>? AppRoles { get; init; }
    }

    private sealed record GraphScopeResolveDto
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("value")] public string? Value { get; init; }
        [JsonPropertyName("adminConsentDisplayName")] public string? AdminConsentDisplayName { get; init; }
    }

    private sealed record GraphAppRoleResolveDto
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("value")] public string? Value { get; init; }
        [JsonPropertyName("displayName")] public string? DisplayName { get; init; }
    }

    // Group Details — private DTOs

    private sealed record GraphMemberEntry
    {
        [JsonPropertyName("@odata.type")]
        public string? OdataType { get; init; }
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
        public DateTimeOffset? CreatedDateTime { get; init; }
    }

    private sealed record GraphDeviceEntry
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? DeviceId { get; init; }
        public string? OperatingSystem { get; init; }
        public string? OsVersion { get; init; }
        public bool? IsCompliant { get; init; }
        public bool? IsManaged { get; init; }
        public string? TrustType { get; init; }
    }

    private sealed record GraphDirectoryAuditEntry
    {
        public string? Id { get; init; }
        public string? ActivityDisplayName { get; init; }
        public string? Category { get; init; }
        public GraphAuditInitiatedBy? InitiatedBy { get; init; }
        public string? Result { get; init; }
        public string? ResultReason { get; init; }
        public DateTimeOffset? ActivityDateTime { get; init; }
        public string? CorrelationId { get; init; }
        public List<GraphAuditTargetResource>? TargetResources { get; init; }
    }

    private sealed record GraphAuditInitiatedBy
    {
        public GraphAuditInitiatorActor? User { get; init; }
        public GraphAuditInitiatorActor? App { get; init; }
    }

    private sealed record GraphAuditInitiatorActor
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
    }

    private sealed record GraphAuditTargetResource
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? Type { get; init; }
    }

    private sealed record GraphAccessReviewDefinition
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? Status { get; init; }
        public DateTimeOffset? StartDate { get; init; }
        public DateTimeOffset? EndDate { get; init; }
        public List<GraphAccessReviewReviewer>? Reviewers { get; init; }
    }

    private sealed record GraphAccessReviewReviewer
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
    }
}



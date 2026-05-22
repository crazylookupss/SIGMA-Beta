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
            "/users", MapUser, select, filter, top, skip, count, cancellationToken);
    }

    public async Task<Result<EntraUser>> GetUserByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        return await GetSingleAsync<GraphUser, EntraUser>(
            $"/users/{Uri.EscapeDataString(id)}", MapUser, select, cancellationToken);
    }

    public async Task<Result<PagedResponse<EntraGroup>>> GetGroupsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<GraphGroup, EntraGroup>(
            "/groups", MapGroup, select, filter, top, skip, count, cancellationToken);
    }

    public async Task<Result<EntraGroup>> GetGroupByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        return await GetSingleAsync<GraphGroup, EntraGroup>(
            $"/groups/{Uri.EscapeDataString(id)}", MapGroup, select, cancellationToken);
    }

    public async Task<Result<PagedResponse<EntraServicePrincipal>>> GetServicePrincipalsAsync(
        string? select, string? filter, int? top, int? skip, bool? count,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync<GraphServicePrincipalDto, EntraServicePrincipal>(
            "/servicePrincipals", MapServicePrincipal, select, filter, top, skip, count, cancellationToken);
    }

    public async Task<Result<EntraServicePrincipal>> GetServicePrincipalByIdAsync(
        string id, string? select, CancellationToken cancellationToken = default)
    {
        return await GetSingleAsync<GraphServicePrincipalDto, EntraServicePrincipal>(
            $"/servicePrincipals/{Uri.EscapeDataString(id)}", MapServicePrincipal, select, cancellationToken);
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
        UserPrincipalName = u.UserPrincipalName,
        GivenName = u.GivenName,
        Surname = u.Surname,
        JobTitle = u.JobTitle,
        Mail = u.Mail,
        MobilePhone = u.MobilePhone,
        OfficeLocation = u.OfficeLocation,
        PreferredLanguage = u.PreferredLanguage,
        BusinessPhone = u.BusinessPhones?.FirstOrDefault(),
        AccountEnabled = u.AccountEnabled,
        UserType = u.UserType,
    };

    private static EntraGroup MapGroup(GraphGroup g) => new()
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
    };

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

    public void Dispose() => _httpClient.Dispose();

    private sealed record GraphUser
    {
        public string? Id { get; init; }
        public string? DisplayName { get; init; }
        public string? UserPrincipalName { get; init; }
        public string? GivenName { get; init; }
        public string? Surname { get; init; }
        public string? JobTitle { get; init; }
        public string? Mail { get; init; }
        public string? MobilePhone { get; init; }
        public string? OfficeLocation { get; init; }
        public string? PreferredLanguage { get; init; }
        public List<string>? BusinessPhones { get; init; }
        public bool? AccountEnabled { get; init; }
        public string? UserType { get; init; }
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
    }

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

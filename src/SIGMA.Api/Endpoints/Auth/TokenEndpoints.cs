using System.Text.Json.Serialization;

namespace SIGMA.Api.Endpoints.Auth;

internal static class TokenEndpoints
{
    public static RouteGroupBuilder MapTokenEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/token", async (
            TokenRequest? request,
            HttpContext httpContext,
            IConfiguration config,
            IHttpClientFactory httpClientFactory) =>
        {
            var req = request ?? new TokenRequest();
            var tenantId = config["Entra:TenantId"];
            var clientId = config["Entra:ClientId"];
            var clientSecret = config["Entra:ClientSecret"];
            var scopes = config.GetSection("Entra:Scopes").Get<string[]>() ?? ["https://graph.microsoft.com/.default"];

            var useClientId = !string.IsNullOrWhiteSpace(req.ClientId) ? req.ClientId : clientId;
            var useClientSecret = !string.IsNullOrWhiteSpace(req.ClientSecret) ? req.ClientSecret : clientSecret;

            if (string.IsNullOrWhiteSpace(useClientId) || string.IsNullOrWhiteSpace(useClientSecret))
            {
                return Results.BadRequest(new
                {
                    error = "invalid_client",
                    error_description = "Client ID and Client Secret are required. Provide them in the request body or configure them in the server."
                });
            }

            var tokenEndpoint = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var requestedScope = !string.IsNullOrWhiteSpace(req.Scope) ? req.Scope : $"api://{useClientId}/.default";

            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = useClientId,
                ["client_secret"] = useClientSecret,
                ["scope"] = requestedScope,
                ["grant_type"] = "client_credentials"
            });

            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsync(tokenEndpoint, formData);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return Results.Problem(
                    statusCode: (int)response.StatusCode,
                    title: "Token acquisition failed",
                    detail: content,
                    instance: tokenEndpoint);
            }

            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content);

            return Results.Ok(tokenResponse);
        })
        .AllowAnonymous()
        .WithName("GetToken")
        .WithTags("Auth")
        .WithDescription("Exchanges client credentials for an access token via Entra ID.");

        return group;
    }
}

internal sealed record TokenRequest
{
    [JsonPropertyName("client_id")]
    public string? ClientId { get; init; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }
}

internal sealed record TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("ext_expires_in")]
    public int ExtExpiresIn { get; init; }

    [JsonPropertyName("scope")]
    public string Scope { get; init; } = string.Empty;
}

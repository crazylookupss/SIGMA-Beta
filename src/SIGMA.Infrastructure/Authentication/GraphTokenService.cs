using Azure.Core;
using Azure.Identity;

namespace SIGMA.Infrastructure.Authentication;

/// <summary>
/// Singleton service that manages Microsoft Graph API OAuth2 tokens.
/// Tokens are shared across all GraphClientService instances (scoped per HTTP request).
/// </summary>
internal sealed class GraphTokenService : IDisposable
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;
    private AccessToken? _currentToken;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public GraphTokenService(EntraAuthConfiguration config)
    {
        _credential = new ClientSecretCredential(
            config.TenantId, config.ClientId, config.ClientSecret,
            new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            });
        _scopes = config.Scopes;
    }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
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

    public void Dispose() => _tokenLock.Dispose();
}

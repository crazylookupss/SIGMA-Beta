using Azure.Identity;
using Microsoft.Graph;
using SIGMA.Infrastructure.Authentication;

namespace SIGMA.Infrastructure.Graph;

internal static class GraphClientFactory
{
    public static GraphServiceClient CreateClient(EntraAuthConfiguration config)
    {
        var credential = new ClientSecretCredential(
            config.TenantId,
            config.ClientId,
            config.ClientSecret,
            new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            });

        return new GraphServiceClient(credential, config.Scopes);
    }
}

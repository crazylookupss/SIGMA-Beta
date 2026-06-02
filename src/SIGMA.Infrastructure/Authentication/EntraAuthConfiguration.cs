namespace SIGMA.Infrastructure.Authentication;

public sealed class EntraAuthConfiguration
{
    public const string SectionName = "AzureAd";

    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = ["https://graph.microsoft.com/.default"];
}

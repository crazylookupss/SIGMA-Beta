using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGMA.Application.Abstractions;
using SIGMA.Application.Caching;
using SIGMA.Application.Governance;
using SIGMA.Application.ProtocolAnalysis.Pipeline;
using SIGMA.Infrastructure.Authentication;
using SIGMA.Infrastructure.Caching;
using SIGMA.Infrastructure.Graph;
using SIGMA.Infrastructure.Governance;
using SIGMA.Infrastructure.ProtocolAnalysis;

namespace SIGMA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var entraConfig = configuration
                .GetSection(EntraAuthConfiguration.SectionName)
                .Get<EntraAuthConfiguration>()!;

        services.AddSingleton(entraConfig);
        services.AddSingleton<GraphTokenService>();
        services.AddMemoryCache();

        // Cache provider (abstraction for future Redis swap)
        services.AddSingleton<ICacheProvider, MemoryCacheProvider>();

        services.AddHttpClient("GraphApi", client =>
        {
            client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IGraphClientService, GraphClientService>();

        // Protocol Analysis pipeline
        services.AddScoped<IDataCollector, GraphDataCollector>();
        services.AddScoped<INormalizationService, DetectionDataNormalizer>();

        // Governance Findings
        services.AddScoped<IGovernanceFindingService, GovernanceFindingService>();

        return services;
    }
}

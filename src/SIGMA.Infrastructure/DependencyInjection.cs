using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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

        // Cache — Redis for multi-instance, in-memory for local dev
        var redisEnabled = configuration.GetValue<bool>("Redis:Enabled");
        if (redisEnabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetValue<string>("Redis:Connection") ?? "localhost:6379";
                options.InstanceName = configuration.GetValue<string>("Redis:InstanceName") ?? "sigma_";
            });
        }
        else
        {
            services.AddMemoryCache();
        }

        // ICacheProvider — same abstraction regardless of backend
        services.AddSingleton<ICacheProvider>(sp =>
            redisEnabled
                ? new RedisCacheProvider(sp.GetRequiredService<IDistributedCache>())
                : new MemoryCacheProvider(sp.GetRequiredService<IMemoryCache>()));

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

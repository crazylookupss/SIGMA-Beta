using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIGMA.Application.Abstractions;
using SIGMA.Infrastructure.Authentication;
using SIGMA.Infrastructure.Graph;

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
        services.AddScoped<IGraphClientService, GraphClientService>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;
using SIGMA.Application.Abstractions;
using SIGMA.Application.Caching;
using SIGMA.Application.Features.ProtocolAnalysis.Abstractions;
using SIGMA.Application.Features.ProtocolAnalysis.Detectors;
using SIGMA.Application.Features.ProtocolAnalysis.Engine;
using SIGMA.Application.ProtocolAnalysis.Engine;
using SIGMA.Application.ProtocolAnalysis.Pipeline;

namespace SIGMA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        var assembly = typeof(DependencyInjection).Assembly;
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

        foreach (var handler in handlerTypes)
        {
            var interfaces = handler.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

            foreach (var iface in interfaces)
            {
                services.AddScoped(iface, handler);
            }
        }

        services.AddProtocolAnalysis();

        return services;
    }

    private static IServiceCollection AddProtocolAnalysis(this IServiceCollection services)
    {
        // Detectors
        services.AddScoped<IProtocolDetector, SamlProtocolDetector>();
        services.AddScoped<IProtocolDetector, OidcProtocolDetector>();
        services.AddScoped<IProtocolDetector, OAuth2ProtocolDetector>();
        services.AddScoped<IProtocolDetector, WsFedProtocolDetector>();
        services.AddScoped<IProtocolDetector, HeaderBasedDetector>();
        services.AddScoped<IProtocolDetector, PasswordSsoDetector>();
        services.AddScoped<IProtocolDetector, LinkedSignOnDetector>();
        services.AddScoped<IProtocolDetector, ScimProvisioningDetector>();

        // Engine
        services.AddScoped<IProtocolAnalysisEngine, ProtocolAnalysisEngine>();

        // Pipeline stages
        services.AddScoped<IProtocolClassifier, ProtocolClassifier>();
        services.AddScoped<IGovernanceAnalyzer, GovernanceAnalyzer>();
        services.AddScoped<IInsightGenerator, InsightGenerator>();

        // Orchestrator
        services.AddScoped<IProtocolAnalysisOrchestrator, ProtocolAnalysisOrchestrator>();

        return services;
    }
}

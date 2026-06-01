using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SIGMA.Application.Abstractions;
using SIGMA.Application.Common;
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

        // FluentValidation validators — registered manually since DI extensions package is not referenced
        services.AddTransient<IValidator<Features.Entra.Applications.ListApplications.ListApplicationsQuery>, ListApplicationsQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.ListGroups.ListGroupsQuery>, ListGroupsQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.ServicePrincipals.ListServicePrincipals.ListServicePrincipalsQuery>, ListServicePrincipalsQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Users.ListUsers.ListUsersQuery>, ListUsersQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Applications.GetApplication.GetApplicationQuery>, GetApplicationQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.GetGroup.GetGroupQuery>, GetGroupQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Users.GetUser.GetUserQuery>, GetUserQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.ServicePrincipals.GetServicePrincipal.GetServicePrincipalQuery>, GetServicePrincipalQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.ServicePrincipals.GetServicePrincipalProxyConfig.GetServicePrincipalProxyConfigQuery>, GetServicePrincipalProxyConfigQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.ServicePrincipals.GetServicePrincipalSsoConfig.GetServicePrincipalSsoConfigQuery>, GetServicePrincipalSsoConfigQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.GetAuditLogs.GetGroupAuditLogsQuery>, GetGroupAuditLogsQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.GetAccessReviews.GetGroupAccessReviewsQuery>, GetGroupAccessReviewsQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.GetDevices.GetGroupDevicesQuery>, GetGroupDevicesQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.GetApplications.GetGroupApplicationsQuery>, GetGroupApplicationsQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.GetOwners.GetGroupOwnersQuery>, GetGroupOwnersQueryValidator>();
        services.AddTransient<IValidator<Features.Entra.Groups.GetMembers.GetGroupMembersQuery>, GetGroupMembersQueryValidator>();
        services.AddTransient<IValidator<Features.ProtocolAnalysis.Queries.GetProtocolAnalysis.GetProtocolAnalysisQuery>, GetProtocolAnalysisQueryValidator>();

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

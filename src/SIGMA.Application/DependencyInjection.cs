using Microsoft.Extensions.DependencyInjection;
using SIGMA.Application.Abstractions;

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

        return services;
    }
}

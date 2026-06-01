using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SIGMA.Application.Abstractions;

namespace SIGMA.Application;

internal sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<TResponse> Send<TResponse>(
        IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();

        // Resolve and run all FluentValidation validators for this query type
        var validatorType = typeof(IValidator<>).MakeGenericType(queryType);
        var validators = serviceProvider.GetServices(validatorType);
        foreach (var v in validators)
        {
            if (v is IValidator validator)
            {
                var context = new ValidationContext<object>(query);
                var result = await validator.ValidateAsync(context, cancellationToken);
                if (!result.IsValid)
                {
                    throw new ValidationException(result.Errors);
                }
            }
        }

        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Handler not found for query: {queryType.Name}");

        var method = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException("Handle method not found on handler");

        var task = (Task<TResponse>)method.Invoke(handler, [query, cancellationToken])!;
        return await task;
    }
}

using SIGMA.Application.Abstractions;

namespace SIGMA.Application;

internal sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public async Task<TResponse> Send<TResponse>(
        IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Handler not found for query: {query.GetType().Name}");

        var method = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException("Handle method not found on handler");

        var task = (Task<TResponse>)method.Invoke(handler, [query, cancellationToken])!;
        return await task;
    }
}

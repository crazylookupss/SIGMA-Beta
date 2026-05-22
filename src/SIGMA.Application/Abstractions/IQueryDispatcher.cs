namespace SIGMA.Application.Abstractions;

public interface IQueryDispatcher
{
    Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}

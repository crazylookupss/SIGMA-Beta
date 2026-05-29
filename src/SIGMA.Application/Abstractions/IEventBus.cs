namespace SIGMA.Application.Abstractions;

public interface IEventBus
{
    Task PublishAsync<T>(string eventType, T data, CancellationToken ct = default) where T : class;
}

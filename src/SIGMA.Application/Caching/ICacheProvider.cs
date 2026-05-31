namespace SIGMA.Application.Caching;

/// <summary>
/// Abstraction for cache operations.
/// Swap MemoryCacheProvider for RedisCacheProvider later without changing consumers.
/// </summary>
public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

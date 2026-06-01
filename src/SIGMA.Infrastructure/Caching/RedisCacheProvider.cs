using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SIGMA.Application.Caching;

namespace SIGMA.Infrastructure.Caching;

/// <summary>
/// IDistributedCache-backed implementation of ICacheProvider.
/// Uses Redis (or any IDistributedCache) for multi-instance, distributed caching.
/// </summary>
internal sealed class RedisCacheProvider : ICacheProvider
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public RedisCacheProvider(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null)
            return default;

        return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(5),
        };

        await _cache.SetAsync(key, bytes, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(key, ct);
    }
}

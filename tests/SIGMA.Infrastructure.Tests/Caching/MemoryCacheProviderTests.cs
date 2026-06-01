using Microsoft.Extensions.Caching.Memory;
using SIGMA.Infrastructure.Caching;
using Xunit;

namespace SIGMA.Infrastructure.Tests.Caching;

public sealed class MemoryCacheProviderTests
{
    private readonly MemoryCacheProvider _cache;

    public MemoryCacheProviderTests()
    {
        var options = new MemoryCacheOptions();
        var memoryCache = new MemoryCache(options);
        _cache = new MemoryCacheProvider(memoryCache);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentKey_ReturnsDefault()
    {
        var result = await _cache.GetAsync<string>("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        const string key = "test_key";
        const string value = "test_value";

        await _cache.SetAsync(key, value);
        var result = await _cache.GetAsync<string>(key);

        Assert.Equal(value, result);
    }

    [Fact]
    public async Task SetAsync_WithCustomTtl_StoresValue()
    {
        const string key = "ttl_key";
        const int value = 42;
        var ttl = TimeSpan.FromSeconds(30);

        await _cache.SetAsync(key, value, ttl);
        var result = await _cache.GetAsync<int>(key);

        Assert.Equal(value, result);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesValue()
    {
        const string key = "remove_key";
        await _cache.SetAsync(key, "value");

        await _cache.RemoveAsync(key);
        var result = await _cache.GetAsync<string>(key);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
    {
        await _cache.RemoveAsync("nonexistent");
    }

    [Fact]
    public async Task SetAsync_OverwriteExistingKey_ReturnsNewValue()
    {
        const string key = "overwrite_key";
        await _cache.SetAsync(key, "old_value");
        await _cache.SetAsync(key, "new_value");

        var result = await _cache.GetAsync<string>(key);

        Assert.Equal("new_value", result);
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_StoresAndRetrieves()
    {
        const string key = "complex_key";
        var value = new TestObject { Id = 1, Name = "Test" };

        await _cache.SetAsync(key, value);
        var result = await _cache.GetAsync<TestObject>(key);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.Name);
    }

    private sealed class TestObject
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}

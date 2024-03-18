using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Common.Services;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(object key, Func<ICacheEntry, Task<T>> cacheEntryBuilder);
    void Invalidate(object key);
}

internal class CacheService : ICacheService
{
    private readonly IMemoryCache _memCache;

    public CacheService(IMemoryCache memCache) => _memCache = memCache;

    public Task<T> GetOrCreateAsync<T>(object key, Func<ICacheEntry, Task<T>> cacheEntryBuilder)
        => _memCache.GetOrCreateAsync(key, cacheEntryBuilder);

    public void Invalidate(object key)
        => _memCache.Remove(key);
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using PeDeYaf.Application.Common.Interfaces;

namespace PeDeYaf.Infrastructure.Cache;

public class RedisCacheService(IDistributedCache cache) : ICacheService
{
    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        return await cache.GetStringAsync(key, ct);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions();
        if (absoluteExpirationRelativeToNow.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        }

        await cache.SetStringAsync(key, value, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct);
    }
}

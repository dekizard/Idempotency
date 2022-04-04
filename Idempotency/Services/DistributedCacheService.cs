using Idempotency.Helpers;
using Idempotency.Interfaces;
using Idempotency.Options;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Idempotency.Services;

public class DistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private static readonly KeyLocker<string> KeyLocker = new();
    private readonly IdempotencyOptions _idempotencyOptions;

    public DistributedCacheService(IDistributedCache distributedCache, IOptions<IdempotencyOptions> idempotencyOptions)    
    {
        _distributedCache = distributedCache;
        _idempotencyOptions = idempotencyOptions.Value;
    }

    public async Task<byte[]> GetOrSet(string key, byte[] value)
    {
        await KeyLocker.Wait(key);
        try
        {
            var cachedData = await _distributedCache.GetAsync(key);
            if (cachedData == null)
            {
                await _distributedCache.SetAsync(key,
                    value,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow =
                            TimeSpan.FromHours(_idempotencyOptions.ExpirationInHours)
                    });

                return value;
            }

            return cachedData;
        }
        finally
        {
            KeyLocker.Release(key);
        }
    }

    public async Task Set(string key, byte[] value)
    {
        await _distributedCache.SetAsync(key,
            value,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromHours(_idempotencyOptions.ExpirationInHours)
            });
    }

    public async Task Remove(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }
}



using Idempotency.Integration.Interfaces;
using Idempotency.Integration.Utils;
using Microsoft.Extensions.Caching.Distributed;

namespace Idempotency.Integration.Services;

public class DistributedCacheService : IDistributedCacheService
{
    private static readonly KeyLocker<string> KeyLocker = new();
    private readonly IDistributedCache _distributedCache;
    //private readonly IdempotencyOptions _idempotencyOptions;


    //public DistributedCacheService(IDistributedCache distributedCache, IOptions<IdempotencyOptions> idempotencyOptions)
    public DistributedCacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
        //_idempotencyOptions = idempotencyOptions.Value;
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
                            TimeSpan.FromHours(24)
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
                    TimeSpan.FromHours(24)
            });
    }

    public async Task Remove(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }
}



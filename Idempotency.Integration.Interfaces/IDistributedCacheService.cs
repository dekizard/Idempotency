namespace Idempotency.Integration.Interfaces;

public interface IDistributedCacheService
{
    Task<byte[]> GetOrSet(string key, byte[] value);
    Task Set(string key, byte[] value);
    Task Remove(string key);
}

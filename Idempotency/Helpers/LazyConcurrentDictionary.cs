using System.Collections.Concurrent;

namespace Idempotency.Helpers;

public class LazyConcurrentDictionary<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _concurrentDictionary;

    public LazyConcurrentDictionary()
    {
        _concurrentDictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
    }

    public TValue GetOrAdd(TKey key, TValue value)
    {
        var lazyResult = _concurrentDictionary.GetOrAdd(key,
            k => new Lazy<TValue>(() => value, LazyThreadSafetyMode.ExecutionAndPublication));
        return lazyResult.Value;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        var success = _concurrentDictionary.TryGetValue(key, out var lazyResult);
        value = success ? lazyResult.Value : default;
        return success;
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        var success = _concurrentDictionary.TryRemove(key, out var lazyResult);
        value = success ? lazyResult.Value : default;
        return success;
    }
}


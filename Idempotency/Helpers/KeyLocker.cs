namespace Idempotency.Helpers;

public class KeyLocker<T>
{
    private static readonly LazyConcurrentDictionary<T, Gate> Gates = new();

    public async Task Wait(T keyToLock)
    {
        await Gates.GetOrAdd(keyToLock, new Gate(1, 1)).AddLock();
    }

    public void Release(T keyToLock)
    {
        if (Gates.TryGetValue(keyToLock, out var gate))
        {
            gate.RemoveLock();
            if (gate.IsOpened && Gates.TryRemove(keyToLock, out gate))
            {
                gate.Dispose();
            }
        }
    }
}

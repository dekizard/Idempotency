namespace Idempotency.Helpers;

public class Gate : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    private int _padLocks;

    public Gate(int initialCount, int maxCount)
    {
        _semaphore = new SemaphoreSlim(initialCount, maxCount);
        _padLocks = 0;
    }

    public bool IsOpened => _padLocks <= 0;

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    public async Task AddLock()
    {
        _padLocks++;
        await _semaphore.WaitAsync();
    }

    public void RemoveLock()
    {
        _padLocks--;
        _semaphore?.Release();
    }
}


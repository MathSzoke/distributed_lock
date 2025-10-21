using System.Collections.Concurrent;

namespace LockDemo.ApiService.Services;

public static class BusyDetector
{
    private static readonly ConcurrentDictionary<string, byte> InFlight = new();

    public static IDisposable? TryEnter(string key)
    {
        if (!InFlight.TryAdd(key, 1)) return null;
        return new Releaser(key);
    }

    private sealed class Releaser(string key) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this._released, 1) == 1) return;
            InFlight.TryRemove(key, out _);
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using Dapper;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Npgsql;
using StackExchange.Redis;

namespace LockDemo.ApiService.Services;

public interface ILockStrategy
{
    Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct);
}

public sealed class NoLockStrategy : ILockStrategy
{
    public Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct) => Task.FromResult<IDisposable?>(new NullHandle());
    private sealed class NullHandle : IDisposable { public void Dispose() { } }
}

public sealed class RedisLockStrategy(IConnectionMultiplexer mux) : ILockStrategy
{
    public async Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct)
    {
        var h = await new RedisDistributedLock(key, mux.GetDatabase()).TryAcquireAsync(timeout, ct);
        return h;
    }
}

public sealed class PostgresProviderLockStrategy(IDistributedLockProvider prov) : ILockStrategy
{
    public async Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct)
    {
        var h = await prov.TryAcquireLockAsync(key, timeout, ct);
        return h;
    }
}

public sealed class PostgresDapperLockStrategy(NpgsqlDataSource ds) : ILockStrategy
{
    public async Task<IDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct)
    {
        var hash = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0);
        var conn = await ds.OpenConnectionAsync(ct);
        var acquired = await conn.ExecuteScalarAsync<bool>("select pg_try_advisory_lock(@key);", new { key = hash });
        if (!acquired) { await conn.DisposeAsync(); return null; }
        return new PgHandle(conn, hash);
    }
    private sealed class PgHandle(NpgsqlConnection c, long k) : IDisposable
    {
        public void Dispose()
        {
            using var _ = c;
            c.Execute("select pg_advisory_unlock(@key);", new { key = k });
        }
    }
}

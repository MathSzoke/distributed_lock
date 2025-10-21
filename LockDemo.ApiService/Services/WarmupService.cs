using Npgsql;
using StackExchange.Redis;

namespace LockDemo.ApiService.Services;

public sealed class WarmupService(IConnectionMultiplexer mux, NpgsqlDataSource ds, ILogger<WarmupService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var db = mux.GetDatabase();
            await db.PingAsync();
            log.LogInformation("Redis ping OK");
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Redis warmup failed");
        }

        try
        {
            await using var c = await ds.OpenConnectionAsync(ct);
            await using var cmd = c.CreateCommand();
            cmd.CommandText = "select 1";
            await cmd.ExecuteScalarAsync(ct);
            log.LogInformation("Postgres warmup OK");
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Postgres warmup failed");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
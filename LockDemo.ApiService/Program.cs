using System.Security.Cryptography;
using System.Text;
using Dapper;
using LockDemo.ApiService.Models;
using LockDemo.ApiService.Services;
using Medallion.Threading;
using Medallion.Threading.Postgres;
using Medallion.Threading.Redis;
using Microsoft.OpenApi.Models;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o => { o.SerializerOptions.PropertyNamingPolicy = null; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Version = "v1", Title = "Lock Demo API", Description = "API for Lock Demo" }));

builder.AddNpgsqlDataSource("lockDemo");
builder.AddRedisClient("redis");

builder.Services.AddKeyedSingleton<IDistributedLockProvider>("postgres", (_, _) =>
{
    var conn = builder.Configuration.GetConnectionString("lockDemo")!;
    return new PostgresDistributedSynchronizationProvider(conn);
});

builder.Services.AddKeyedSingleton<IDistributedLockProvider>("redis-provider", (sp, _) =>
{
    var mux = sp.GetRequiredService<IConnectionMultiplexer>();
    return new RedisDistributedSynchronizationProvider(mux.GetDatabase());
});

builder.Services.AddHostedService<WarmupService>();

builder.Services.AddCors();
builder.Services.AddSingleton<WorkSimulator>();
builder.Services.AddSingleton<RaceCoordinator>();

var app = builder.Build();

app.UseCors(_ => _.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/run", async (
    RunRequest req,
    NpgsqlDataSource dataSource,
    [FromKeyedServices("postgres")] IDistributedLockProvider pgProvider,
    [FromKeyedServices("redis-provider")] IDistributedLockProvider redisProvider,
    IConnectionMultiplexer mux,
    CancellationToken ct) =>
{
    static ILockStrategy SelectStrategy(
        string provider,
        IConnectionMultiplexer mux,
        IDistributedLockProvider pgProvider,
        NpgsqlDataSource ds)
        => provider.ToLowerInvariant() switch
        {
            "redis" => new RedisLockStrategy(mux),
            "postgres" => new PostgresProviderLockStrategy(pgProvider),
            "dapper" => new PostgresDapperLockStrategy(ds),
            _ => new NoLockStrategy()
        };

    var tracker = new StepTracker();
    tracker.Mark("Start");
    await Task.Delay(300, ct);
    tracker.Mark("Critical");

    if (!req.UseLock)
    {
        using var busy = BusyDetector.TryEnter(req.Key);
        if (busy is null) return Results.StatusCode(409);
        await Task.Delay(req.WorkMs, ct);
        tracker.Mark("Done");
        return Results.Ok(new RunResult
        {
            Actor = req.Actor,
            UseLock = req.UseLock,
            Provider = req.Provider,
            Key = req.Key,
            Steps = [.. tracker.Steps],
            TimestampsMs = [.. tracker.Timestamps]
        });
    }

    var strategy = SelectStrategy(req.Provider, mux, pgProvider, dataSource);
    using var handle = await strategy.TryAcquireAsync(
        req.Key,
        TimeSpan.FromMilliseconds(req.LockTimeoutMs),
        ct);

    if (handle is null) return Results.StatusCode(409);

    await Task.Delay(req.WorkMs, ct);
    tracker.Mark("Done");

    return Results.Ok(new RunResult
    {
        Actor = req.Actor,
        UseLock = req.UseLock,
        Provider = req.Provider,
        Key = req.Key,
        Steps = [.. tracker.Steps],
        TimestampsMs = [.. tracker.Timestamps]
    });
});

app.MapPost("/locking-demo", async (NpgsqlDataSource dataSource) =>
{
    await using var connection = await dataSource.OpenConnectionAsync();
    static long HashKey(string key) => BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0);
    var key = HashKey("nightly-report");
    var acquired = await connection.ExecuteScalarAsync<bool>("select pg_try_advisory_lock(@key);", new { key });
    if (!acquired) return Results.Conflict(new { message = "Another process is already running the nightly report." });
    try { await Task.Delay(5000); } finally { await connection.ExecuteAsync("select pg_advisory_unlock(@key);", new { key }); }
    return Results.Ok(new { message = "Nightly report completed successfully." });
});

app.MapPost("/locking-demo/postgres", async (
    [FromKeyedServices("postgres")] IDistributedLockProvider distributed) =>
{
    var h = distributed.TryAcquireLock("nightly-report");
    if (h is null) return Results.Conflict(new { message = "Another process is already running the nightly report." });
    using (h) { await Task.Delay(5000); }
    return Results.Ok(new { message = "Nightly report completed succesfully." });
});

app.MapPost("/locking-demo/redis", async (
    [FromKeyedServices("redis-provider")] IDistributedLockProvider distributed) =>
{
    var h = distributed.TryAcquireLock("nightly-report");
    if (h is null) return Results.Conflict(new { message = "Another process is already running the nightly report." });
    using (h) { await Task.Delay(5000); }
    return Results.Ok(new { message = "Nightly report completed succesfully." });
});

app.Run();

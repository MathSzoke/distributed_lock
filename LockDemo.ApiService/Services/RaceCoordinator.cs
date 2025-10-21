using LockDemo.ApiService.Models;

namespace LockDemo.ApiService.Services;

public sealed class RaceCoordinator(WorkSimulator sim)
{
    public async Task<(RunResult a, RunResult b, int status)> RunAsync(RunRequest req, Func<string, ILockStrategy> lockFactory, CancellationToken ct)
    {
        var aTracker = new StepTracker();
        var bTracker = new StepTracker();

        async Task<RunResult> Exec(string actor)
        {
            var tracker = actor == "X" ? aTracker : bTracker;
            if (!req.UseLock)
            {
                await sim.ExecuteAsync(300, req.WorkMs, tracker, ct);
                return ToResult(actor, req, tracker);
            }
            var strategy = lockFactory(req.Provider);
            using var handle = await strategy.TryAcquireAsync(req.Key, TimeSpan.FromMilliseconds(req.LockTimeoutMs), ct);
            if (handle is null)
            {
                tracker.Mark("Start");
                tracker.Mark("Blocked");
                return ToBlocked(actor, req, tracker);
            }
            await sim.ExecuteAsync(300, req.WorkMs, tracker, ct);
            return ToResult(actor, req, tracker);
        }

        var ta = Exec("X");
        var tb = Task.Delay(40, ct).ContinueWith(_ => Exec("Y"), ct).Unwrap();
        var results = await Task.WhenAll(ta, tb);
        var status = results.Any(x => x.Steps.Contains("Blocked")) ? 409 : 200;
        return (results[0], results[1], status);
    }

    private static RunResult ToResult(string actor, RunRequest req, IStepTracker t)
    {
        return new RunResult
        {
            Actor = actor,
            UseLock = req.UseLock,
            Provider = req.Provider,
            Key = req.Key,
            Steps = [.. t.Steps],
            TimestampsMs = [.. t.Timestamps]
        };
    }

    private static RunResult ToBlocked(string actor, RunRequest req, IStepTracker t)
    {
        return new RunResult
        {
            Actor = actor,
            UseLock = req.UseLock,
            Provider = req.Provider,
            Key = req.Key,
            Steps = [.. t.Steps],
            TimestampsMs = [.. t.Timestamps]
        };
    }
}

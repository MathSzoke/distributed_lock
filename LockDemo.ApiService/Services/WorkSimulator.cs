namespace LockDemo.ApiService.Services;

public sealed class WorkSimulator
{
    public async Task ExecuteAsync(int preMs, int workMs, IStepTracker tracker, CancellationToken ct)
    {
        tracker.Mark("Start");
        await Task.Delay(preMs, ct);
        tracker.Mark("Critical");
        await Task.Delay(workMs, ct);
        tracker.Mark("Done");
    }
}

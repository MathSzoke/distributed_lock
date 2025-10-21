using System.Diagnostics;

namespace LockDemo.ApiService.Services;

public interface IStepTracker
{
    void Mark(string step);
    IReadOnlyList<string> Steps { get; }
    IReadOnlyList<long> Timestamps { get; }
}

public sealed class StepTracker : IStepTracker
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly List<string> _steps = [];
    private readonly List<long> _times = [];
    public void Mark(string step)
    { 
        this._steps.Add(step); 
        this._times.Add(this._sw.ElapsedMilliseconds);
    }
    public IReadOnlyList<string> Steps => this._steps;
    public IReadOnlyList<long> Timestamps => this._times;
}

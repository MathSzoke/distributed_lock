namespace LockDemo.ApiService.Models;

public sealed class RunResult
{
    public string Actor { get; set; } = "";
    public bool UseLock { get; set; }
    public string Provider { get; set; } = "";
    public string Key { get; set; } = "";
    public List<string> Steps { get; set; } = [];
    public List<long> TimestampsMs { get; set; } = [];
}

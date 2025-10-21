namespace LockDemo.ApiService.Models;

public sealed class RunRequest
{
    public string Actor { get; set; } = "Anon";
    public bool UseLock { get; set; }
    public string Provider { get; set; } = "redis";
    public string Key { get; set; } = "order:demo";
    public int WorkMs { get; set; } = 2000;
    public int LockTimeoutMs { get; set; } = 3000;
}

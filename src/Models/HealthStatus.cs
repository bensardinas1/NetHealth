namespace NetHealth.Models;

public enum HealthState
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy
}

public sealed class HealthStatus
{
    public string TargetName { get; init; } = "";
    public HealthState State { get; init; }
    public long LatencyMs { get; init; }
    public string? Detail { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}

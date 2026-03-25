using System.Collections.Concurrent;

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

/// <summary>Tracks rolling statistics for a single target.</summary>
public sealed class TargetStats
{
    private readonly ConcurrentQueue<(DateTime Utc, long LatencyMs)> _samples = new();
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    public long LastLatencyMs { get; private set; }
    public DateTime? LastFailureUtc { get; private set; }

    public void Record(HealthStatus status)
    {
        var now = status.TimestampUtc;
        LastLatencyMs = status.LatencyMs;

        if (status.State == HealthState.Unhealthy)
            LastFailureUtc = now;

        _samples.Enqueue((now, status.LatencyMs));

        // Trim samples older than 5 minutes
        while (_samples.TryPeek(out var oldest) && now - oldest.Utc > Window)
            _samples.TryDequeue(out _);
    }

    public double GetRollingAverageMs()
    {
        var items = _samples.ToArray();
        return items.Length == 0 ? 0 : items.Average(s => s.LatencyMs);
    }
}

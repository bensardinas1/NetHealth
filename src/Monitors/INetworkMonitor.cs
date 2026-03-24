using NetHealth.Models;

namespace NetHealth.Monitors;

public interface INetworkMonitor
{
    string TargetName { get; }
    Task<HealthStatus> CheckAsync(CancellationToken ct);
}

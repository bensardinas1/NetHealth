using System.Diagnostics;
using System.Net;
using NetHealth.Models;

namespace NetHealth.Monitors;

public sealed class DnsMonitor : INetworkMonitor
{
    private readonly string _resolve;
    private readonly int _timeoutMs;

    public string TargetName { get; }

    public DnsMonitor(TargetConfig config)
    {
        TargetName = config.Name;
        _resolve = config.Resolve ?? throw new ArgumentException("Resolve domain is required for DNS monitor");
        _timeoutMs = config.TimeoutMs;
    }

    public async Task<HealthStatus> CheckAsync(CancellationToken ct)
    {
        try
        {
            var sw = Stopwatch.StartNew();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);

            var addresses = await Dns.GetHostAddressesAsync(_resolve, cts.Token);
            sw.Stop();

            if (addresses.Length == 0)
            {
                return new HealthStatus
                {
                    TargetName = TargetName,
                    State = HealthState.Unhealthy,
                    LatencyMs = sw.ElapsedMilliseconds,
                    Detail = $"No addresses returned for {_resolve}"
                };
            }

            return new HealthStatus
            {
                TargetName = TargetName,
                State = HealthState.Healthy,
                LatencyMs = sw.ElapsedMilliseconds,
                Detail = $"Resolved {_resolve} → {addresses[0]} in {sw.ElapsedMilliseconds}ms"
            };
        }
        catch (OperationCanceledException)
        {
            return new HealthStatus
            {
                TargetName = TargetName,
                State = HealthState.Unhealthy,
                Detail = $"DNS resolution timed out after {_timeoutMs}ms"
            };
        }
        catch (Exception ex)
        {
            return new HealthStatus
            {
                TargetName = TargetName,
                State = HealthState.Unhealthy,
                Detail = ex.Message
            };
        }
    }
}

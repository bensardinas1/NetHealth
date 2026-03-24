using System.Diagnostics;
using System.Net.NetworkInformation;
using NetHealth.Models;

namespace NetHealth.Monitors;

public sealed class PingMonitor : INetworkMonitor
{
    private readonly string _host;
    private readonly int _timeoutMs;
    private readonly int _thresholdMs;

    public string TargetName { get; }

    public PingMonitor(TargetConfig config)
    {
        TargetName = config.Name;
        _host = config.Host ?? throw new ArgumentException("Host is required for ping monitor");
        _timeoutMs = config.TimeoutMs;
        _thresholdMs = config.ThresholdMs;
    }

    public async Task<HealthStatus> CheckAsync(CancellationToken ct)
    {
        try
        {
            using var ping = new Ping();
            var sw = Stopwatch.StartNew();
            var reply = await ping.SendPingAsync(_host, _timeoutMs);
            sw.Stop();

            if (reply.Status != IPStatus.Success)
            {
                return new HealthStatus
                {
                    TargetName = TargetName,
                    State = HealthState.Unhealthy,
                    LatencyMs = sw.ElapsedMilliseconds,
                    Detail = $"Ping failed: {reply.Status}"
                };
            }

            var state = reply.RoundtripTime <= _thresholdMs
                ? HealthState.Healthy
                : HealthState.Degraded;

            return new HealthStatus
            {
                TargetName = TargetName,
                State = state,
                LatencyMs = reply.RoundtripTime,
                Detail = $"{reply.RoundtripTime}ms (threshold: {_thresholdMs}ms)"
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

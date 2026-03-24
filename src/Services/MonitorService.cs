using NetHealth.Models;
using NetHealth.Monitors;

namespace NetHealth.Services;

public sealed class MonitorService : IDisposable
{
    private readonly List<(INetworkMonitor Monitor, int IntervalSeconds)> _monitors = [];
    private readonly Dictionary<string, HealthStatus> _lastResults = [];
    private CancellationTokenSource? _cts;
    private readonly List<Task> _pollTasks = [];

    public event Action<IReadOnlyDictionary<string, HealthStatus>>? StatusChanged;

    public void Configure(AppConfig config)
    {
        _monitors.Clear();

        foreach (var target in config.Targets.Where(t => t.Enabled))
        {
            INetworkMonitor? monitor = target.Type.ToLowerInvariant() switch
            {
                "ping" => new PingMonitor(target),
                "dns" => new DnsMonitor(target),
                "http" => new HttpMonitor(target),
                _ => null
            };

            if (monitor != null)
                _monitors.Add((monitor, target.PollIntervalSeconds));
        }
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _pollTasks.Clear();

        foreach (var (monitor, interval) in _monitors)
            _pollTasks.Add(PollLoopAsync(monitor, interval, _cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _pollTasks.Clear();
    }

    public HealthState GetOverallState()
    {
        if (_lastResults.Count == 0)
            return HealthState.Unknown;

        if (_lastResults.Values.Any(r => r.State == HealthState.Unhealthy))
            return HealthState.Unhealthy;

        if (_lastResults.Values.Any(r => r.State == HealthState.Degraded))
            return HealthState.Degraded;

        return HealthState.Healthy;
    }

    public IReadOnlyDictionary<string, HealthStatus> GetLastResults()
        => _lastResults;

    private async Task PollLoopAsync(INetworkMonitor monitor, int intervalSeconds, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await monitor.CheckAsync(ct);
                lock (_lastResults)
                    _lastResults[monitor.TargetName] = result;
                StatusChanged?.Invoke(_lastResults);
            }
            catch (OperationCanceledException) { break; }

            try { await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}

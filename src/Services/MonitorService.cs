using NetHealth.Models;
using NetHealth.Monitors;

namespace NetHealth.Services;

public sealed class MonitorService : IDisposable
{
    private readonly List<INetworkMonitor> _monitors = [];
    private readonly Dictionary<string, HealthStatus> _lastResults = [];
    private CancellationTokenSource? _cts;
    private Task? _pollTask;

    public int PollIntervalSeconds { get; set; } = 30;

    public event Action<IReadOnlyDictionary<string, HealthStatus>>? StatusChanged;

    public void Configure(AppConfig config)
    {
        _monitors.Clear();
        PollIntervalSeconds = config.PollIntervalSeconds;

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
                _monitors.Add(monitor);
        }
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _pollTask = PollLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _pollTask = null;
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

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await RunChecksAsync(ct);
            try { await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunChecksAsync(CancellationToken ct)
    {
        var tasks = _monitors.Select(async m =>
        {
            var result = await m.CheckAsync(ct);
            return (m.TargetName, result);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (name, status) in results)
            _lastResults[name] = status;

        StatusChanged?.Invoke(_lastResults);
    }

    public void Dispose()
    {
        Stop();
    }
}

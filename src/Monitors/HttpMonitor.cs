using System.Diagnostics;
using NetHealth.Models;

namespace NetHealth.Monitors;

public sealed class HttpMonitor : INetworkMonitor
{
    private readonly string _url;
    private readonly int _timeoutMs;
    private readonly int _expectedStatus;

    private static readonly HttpClient SharedClient = new()
    {
        DefaultRequestHeaders = { { "User-Agent", "NetHealth/0.1" } }
    };

    public string TargetName { get; }

    public HttpMonitor(TargetConfig config)
    {
        TargetName = config.Name;
        _url = config.Url ?? throw new ArgumentException("URL is required for HTTP monitor");
        _timeoutMs = config.TimeoutMs;
        _expectedStatus = config.ExpectedStatusCode;
    }

    public async Task<HealthStatus> CheckAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(_timeoutMs);

            var sw = Stopwatch.StartNew();
            using var request = new HttpRequestMessage(HttpMethod.Head, _url);
            using var response = await SharedClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            sw.Stop();

            var statusCode = (int)response.StatusCode;
            var state = statusCode == _expectedStatus
                ? HealthState.Healthy
                : HealthState.Degraded;

            return new HealthStatus
            {
                TargetName = TargetName,
                State = state,
                LatencyMs = sw.ElapsedMilliseconds,
                Detail = $"HTTP {statusCode} in {sw.ElapsedMilliseconds}ms"
            };
        }
        catch (OperationCanceledException)
        {
            return new HealthStatus
            {
                TargetName = TargetName,
                State = HealthState.Unhealthy,
                Detail = $"HTTP request timed out after {_timeoutMs}ms"
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

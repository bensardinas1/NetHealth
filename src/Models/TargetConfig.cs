using System.Text.Json.Serialization;

namespace NetHealth.Models;

public sealed class AppConfig
{
    public bool ShowOverlay { get; set; } = true;
    public bool NotifyOnChange { get; set; } = true;
    public List<TargetConfig> Targets { get; set; } = [];
}

public sealed class TargetConfig
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "ping";
    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 30;

    // Ping
    public string? Host { get; set; }
    public int TimeoutMs { get; set; } = 2000;
    public int ThresholdMs { get; set; } = 100;

    // DNS
    public string? Resolve { get; set; }

    // HTTP
    public string? Url { get; set; }
    public int ExpectedStatusCode { get; set; } = 200;
    public bool FollowRedirects { get; set; } = true;

    [JsonIgnore]
    public string DisplayAddress => Type.ToLowerInvariant() switch
    {
        "ping" => Host ?? "",
        "dns" => Resolve ?? "",
        "http" => Url ?? "",
        _ => ""
    };
}

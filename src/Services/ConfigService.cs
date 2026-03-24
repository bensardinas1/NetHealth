using System.Net.NetworkInformation;
using System.Text.Json;
using NetHealth.Models;

namespace NetHealth.Services;

public static class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static AppConfig Load()
    {
        var configPath = GetConfigPath();
        if (!File.Exists(configPath))
            return new AppConfig();

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();

        // Resolve "auto" gateway placeholders
        foreach (var t in config.Targets)
        {
            if (string.Equals(t.Host, "auto", StringComparison.OrdinalIgnoreCase)
                && t.Type.Equals("ping", StringComparison.OrdinalIgnoreCase))
            {
                t.Host = DetectDefaultGateway() ?? "192.168.1.1";
            }
        }

        return config;
    }

    public static void Save(AppConfig config)
    {
        // Always save to the local override file
        var appDir = AppContext.BaseDirectory;
        var configPath = Path.Combine(appDir, "config", "targets.local.json");
        var dir = Path.GetDirectoryName(configPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(configPath, json);
    }

    private static string GetConfigPath()
    {
        // Check for local override first, then bundled config
        var appDir = AppContext.BaseDirectory;
        var localPath = Path.Combine(appDir, "config", "targets.local.json");
        if (File.Exists(localPath))
            return localPath;

        return Path.Combine(appDir, "config", "targets.json");
    }

    public static string? DetectDefaultGateway()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                .Select(g => g.Address.ToString())
                .FirstOrDefault(a => a != "0.0.0.0" && !a.Contains(':'));
        }
        catch
        {
            return null;
        }
    }
}

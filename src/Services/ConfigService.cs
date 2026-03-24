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
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
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
}

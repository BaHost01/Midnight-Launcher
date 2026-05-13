using System;
using System.IO;
using Newtonsoft.Json;

namespace Midnight_Launcher;

public class LauncherConfig
{
    public bool ExperimentalUi { get; set; } = false;
    public int SelectedRam { get; set; } = 4096;
    public string Theme { get; set; } = "Midnight";
    public string GamePath { get; set; } = "./game";
}

public static class ConfigService
{
    private static readonly string ConfigPath = "config.json";

    public static LauncherConfig Load()
    {
        if (!File.Exists(ConfigPath))
        {
            var defaultConfig = new LauncherConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<LauncherConfig>(json) ?? new LauncherConfig();
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load config, using defaults", ex);
            return new LauncherConfig();
        }
    }

    public static void Save(LauncherConfig config)
    {
        try
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to save config", ex);
        }
    }
}

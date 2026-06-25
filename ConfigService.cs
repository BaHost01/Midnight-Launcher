using System;
using System.IO;
using Newtonsoft.Json;

namespace Midnight_Launcher;

public class LauncherConfig
{
    public bool ExperimentalUi { get; set; } = false;
    public int SelectedRam { get; set; } = 4096;
    public string Theme { get; set; } = "Midnight";
    public string GamePath { get; set; } = LauncherPaths.GameDirectory;
}

public static class ConfigService
{
    public static LauncherConfig Load()
    {
        LauncherPaths.Ensure();

        if (!File.Exists(LauncherPaths.ConfigPath))
        {
            var defaultConfig = new LauncherConfig
            {
                GamePath = LauncherPaths.GameDirectory
            };
            
            // Auto-detect .minecraft in AppData
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var defaultMcPath = Path.Combine(appData, ".minecraft");
                if (Directory.Exists(defaultMcPath))
                {
                    defaultConfig.GamePath = defaultMcPath;
                    LoggingService.Info($"Auto-detected .minecraft path: {defaultMcPath}");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error("Failed to auto-detect .minecraft path", ex);
            }

            Save(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(LauncherPaths.ConfigPath);
            var config = JsonConvert.DeserializeObject<LauncherConfig>(json) ?? new LauncherConfig();
            if (string.IsNullOrWhiteSpace(config.GamePath))
                config.GamePath = LauncherPaths.GameDirectory;
            return config;
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to load config, using defaults", ex);
            return new LauncherConfig { GamePath = LauncherPaths.GameDirectory };
        }
    }

    public static void Save(LauncherConfig config)
    {
        try
        {
            LauncherPaths.Ensure();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(LauncherPaths.ConfigPath, json);
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to save config", ex);
        }
    }
}

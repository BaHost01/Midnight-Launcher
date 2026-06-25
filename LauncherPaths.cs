using System;
using System.IO;

namespace Midnight_Launcher;

internal static class LauncherPaths
{
    internal static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MidnightLauncher");

    internal static readonly string LogsDirectory = Path.Combine(DataDirectory, "logs");
    internal static readonly string CacheDirectory = Path.Combine(DataDirectory, "cache");
    internal static readonly string UpdatesDirectory = Path.Combine(CacheDirectory, "updates");
    internal static readonly string GameDirectory = Path.Combine(DataDirectory, "game");
    internal static readonly string ConfigPath = Path.Combine(DataDirectory, "config.json");
    internal static readonly string AccountsPath = Path.Combine(DataDirectory, "accounts.json");
    internal static readonly string SettingsPath = Path.Combine(DataDirectory, "Settings.yaml");
    internal static readonly string TokensPath = Path.Combine(DataDirectory, "Tokens.json");
    internal static readonly string UpdaterScriptPath = Path.Combine(DataDirectory, "updater.ps1");

    static LauncherPaths()
    {
        Ensure();
    }

    internal static void Ensure()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(UpdatesDirectory);
        Directory.CreateDirectory(GameDirectory);
    }
}

using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Midnight_Launcher;

public class SettingsYaml
{
    public string LauncherVersion { get; set; } = "v1.1.9";
    public string Environment { get; set; } = "Production";
    public bool AutoUpdateEnabled { get; set; } = true;
    public string UpdateChannel { get; set; } = "Stable";
    public List<string> RequiredAssets { get; set; } = new() { "app_icon.png", "play.png" };
    public Dictionary<string, string> ApiEndpoints { get; set; } = new()
    {
        { "GitHub", "https://api.github.com" },
        { "MojangNews", "https://launchercontent.mojang.com/news.json" },
        { "Modrinth", "https://api.modrinth.com/v2" }
    };
}

public class SettingsService
{
    private static readonly string SettingsPath = "Settings.yaml";
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static SettingsYaml Load()
    {
        if (!File.Exists(SettingsPath))
        {
            var defaultSettings = new SettingsYaml();
            Save(defaultSettings);
            return defaultSettings;
        }

        try
        {
            var yaml = File.ReadAllText(SettingsPath);
            return Deserializer.Deserialize<SettingsYaml>(yaml);
        }
        catch
        {
            return new SettingsYaml();
        }
    }

    public static void Save(SettingsYaml settings)
    {
        try
        {
            var yaml = Serializer.Serialize(settings);
            File.WriteAllText(SettingsPath, yaml);
        }
        catch { }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrayAlarmWatcher.Configuration;

public sealed class AppConfig
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("regionId")]
    public string RegionId { get; set; } = string.Empty;

    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TrayAlarmWatcher");

    private static readonly string ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
    private static readonly string RegionsLogPath = Path.Combine(ConfigDirectory, "regions-log.json");

    public static string DirectoryPath => ConfigDirectory;

    public static string FilePath => ConfigFilePath;

    public static string RegionsLogFilePath => RegionsLogPath;

    public static AppConfig? Load()
    {
        if (!File.Exists(ConfigFilePath))
        {
            return null;
        }

        var json = File.ReadAllText(ConfigFilePath);
        return JsonSerializer.Deserialize<AppConfig>(json);
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }

    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(RegionId);
}

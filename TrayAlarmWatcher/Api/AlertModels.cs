using System.Text.Json.Serialization;

namespace TrayAlarmWatcher.Api;

public sealed class AlertRegionModel
{
    [JsonPropertyName("regionId")]
    public string RegionId { get; set; } = string.Empty;

    [JsonPropertyName("regionType")]
    public string RegionType { get; set; } = string.Empty;

    [JsonPropertyName("regionName")]
    public string RegionName { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; }

    // API інколи повертає null замість порожнього масиву, коли активних тривог немає.
    [JsonPropertyName("activeAlerts")]
    public List<AlertItem>? ActiveAlerts { get; set; }
}

public sealed class AlertItem
{
    [JsonPropertyName("regionId")]
    public string RegionId { get; set; } = string.Empty;

    [JsonPropertyName("regionType")]
    public string RegionType { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("lastUpdate")]
    public DateTime? LastUpdate { get; set; }
}

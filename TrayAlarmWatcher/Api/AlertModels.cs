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

    // Може бути null, коли API не надає деталізації рівня для цього алерту.
    [JsonPropertyName("activeAlertLevels")]
    public List<AlertLevelWithReason>? ActiveAlertLevels { get; set; }
}

public sealed class AlertLevelWithReason
{
    // Enum "AlertLevel" у API: "Red" | "Yellow".
    [JsonPropertyName("alertLevel")]
    public string AlertLevel { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
}

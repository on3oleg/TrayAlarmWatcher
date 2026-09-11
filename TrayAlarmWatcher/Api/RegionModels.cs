using System.Text.Json.Serialization;

namespace TrayAlarmWatcher.Api;

public sealed class RegionsResponse
{
    [JsonPropertyName("states")]
    public List<Region> States { get; set; } = new();
}

public sealed class Region
{
    [JsonPropertyName("regionId")]
    public string RegionId { get; set; } = string.Empty;

    [JsonPropertyName("regionName")]
    public string RegionName { get; set; } = string.Empty;

    [JsonPropertyName("regionType")]
    public string RegionType { get; set; } = string.Empty;

    [JsonPropertyName("regionChildIds")]
    public List<Region> RegionChildIds { get; set; } = new();
}

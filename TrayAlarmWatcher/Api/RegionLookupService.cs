using System.Text.Json;
using TrayAlarmWatcher.Configuration;

namespace TrayAlarmWatcher.Api;

public readonly record struct RegionLookupResult(bool Success, string? RegionId, string? RegionName)
{
    public static RegionLookupResult Found(string regionId, string regionName) => new(true, regionId, regionName);

    public static RegionLookupResult NotFound() => new(false, null, null);
}

public sealed class RegionLookupService
{
    private const string DistrictRegionType = "District";
    private const string BuchaNameFragment = "Бучанськ";

    private readonly RegionsApiClient _client;

    public RegionLookupService(RegionsApiClient client)
    {
        _client = client;
    }

    public async Task<RegionLookupResult> FindBuchaDistrictAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var regions = await FetchRegionsAsync(apiKey, cancellationToken);

        var match = regions.States
            .SelectMany(FlattenRegions)
            .FirstOrDefault(r =>
                string.Equals(r.RegionType, DistrictRegionType, StringComparison.OrdinalIgnoreCase) &&
                r.RegionName.Contains(BuchaNameFragment, StringComparison.OrdinalIgnoreCase));

        return match is null
            ? RegionLookupResult.NotFound()
            : RegionLookupResult.Found(match.RegionId, match.RegionName);
    }

    /// <summary>Повне дерево область → район → громада, для меню ручного вибору регіону.</summary>
    public async Task<List<Region>> GetStatesAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var regions = await FetchRegionsAsync(apiKey, cancellationToken);
        return regions.States;
    }

    private async Task<RegionsResponse> FetchRegionsAsync(string apiKey, CancellationToken cancellationToken)
    {
        var rawJson = await _client.GetRegionsRawAsync(apiKey, cancellationToken);
        LogRegionsForDiagnostics(rawJson);
        return JsonSerializer.Deserialize<RegionsResponse>(rawJson) ?? new RegionsResponse();
    }

    private static IEnumerable<Region> FlattenRegions(Region region)
    {
        yield return region;
        foreach (var child in region.RegionChildIds)
        {
            foreach (var descendant in FlattenRegions(child))
            {
                yield return descendant;
            }
        }
    }

    private static void LogRegionsForDiagnostics(string rawJson)
    {
        // Пишемо лише при першому запуску, щоб мати з чим звірити вручну, якщо пошук за назвою не спрацює.
        if (File.Exists(AppConfig.RegionsLogFilePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AppConfig.RegionsLogFilePath)!);

        using var document = JsonDocument.Parse(rawJson);
        var pretty = JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(AppConfig.RegionsLogFilePath, pretty);
    }
}

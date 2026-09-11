using System.Text.Json;
using TrayAlarmWatcher.Configuration;

namespace TrayAlarmWatcher.Api;

public sealed class RegionLookupService
{
    private readonly RegionsApiClient _client;

    public RegionLookupService(RegionsApiClient client)
    {
        _client = client;
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

    private static void LogRegionsForDiagnostics(string rawJson)
    {
        // Пишемо лише при першому запуску, щоб мати з чим звірити вручну за потреби.
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

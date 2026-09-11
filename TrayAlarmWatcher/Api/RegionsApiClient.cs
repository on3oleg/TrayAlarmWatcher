using System.Net.Http.Headers;

namespace TrayAlarmWatcher.Api;

public sealed class RegionsApiClient
{
    private const string RegionsUrl = "https://api.ukrainealarm.com/api/v3/regions";

    private readonly HttpClient _httpClient;

    public RegionsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetRegionsRawAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RegionsUrl);

        // API приймає голий ключ у заголовку Authorization, без префіксу "Bearer ".
        // TryAddWithoutValidation потрібен, бо значення не є валідною AuthenticationHeaderValue схемою.
        request.Headers.TryAddWithoutValidation("Authorization", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}

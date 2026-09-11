using System.Net.Http.Headers;
using System.Text.Json;

namespace TrayAlarmWatcher.Api;

public sealed class AlertsApiClient
{
    private const string AlertsBaseUrl = "https://api.ukrainealarm.com/api/v3/alerts";

    private readonly HttpClient _httpClient;

    public AlertsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<AlertRegionModel>> GetAlertsForRegionAsync(
        string apiKey, string regionId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{AlertsBaseUrl}/{regionId}");
        request.Headers.TryAddWithoutValidation("Authorization", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApiRequestException($"Мережева помилка: {ex.Message}", statusCode: null, retryAfter: null, ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiRequestException(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
                    response.StatusCode,
                    GetRetryAfter(response));
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<AlertRegionModel>>(json) ?? new List<AlertRegionModel>();
        }
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter.Date is { } date)
        {
            var wait = date - DateTimeOffset.Now;
            return wait > TimeSpan.Zero ? wait : null;
        }

        return null;
    }
}

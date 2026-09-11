using System.Net;
using TrayAlarmWatcher.Api;
using TrayAlarmWatcher.Models;

namespace TrayAlarmWatcher.Services;

public sealed class AlarmStatusChecker
{
    // 30с → 60с → 120с: після вичерпання останньої спроби статус стає "невідомо".
    private static readonly TimeSpan[] BackoffDelays =
    {
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120)
    };

    private readonly AlertsApiClient _alertsApiClient;

    public AlarmStatusChecker(AlertsApiClient alertsApiClient)
    {
        _alertsApiClient = alertsApiClient;
    }

    public async Task<AlarmStatusSnapshot> CheckAsync(string apiKey, string regionId, CancellationToken cancellationToken = default)
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt <= BackoffDelays.Length; attempt++)
        {
            try
            {
                var regions = await _alertsApiClient.GetAlertsForRegionAsync(apiKey, regionId, cancellationToken);
                var hasActiveAlert = regions.Any(r => r.ActiveAlerts.Count > 0);

                return new AlarmStatusSnapshot(
                    hasActiveAlert ? AlarmStatus.Alarm : AlarmStatus.Calm,
                    DateTime.Now,
                    null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                FileLogger.LogError(
                    $"Перевірка статусу тривоги (regionId={regionId}), спроба {attempt + 1}/{BackoffDelays.Length + 1}: {ex.Message}");

                if (attempt < BackoffDelays.Length)
                {
                    await Task.Delay(GetRetryDelay(ex, BackoffDelays[attempt]), cancellationToken);
                }
            }
        }

        FileLogger.LogError($"Усі спроби перевірки статусу тривоги (regionId={regionId}) провалились: {lastError?.Message}");
        return new AlarmStatusSnapshot(AlarmStatus.Unknown, DateTime.Now, lastError?.Message);
    }

    private static TimeSpan GetRetryDelay(Exception ex, TimeSpan defaultDelay)
    {
        if (ex is ApiRequestException { StatusCode: HttpStatusCode.TooManyRequests } apiEx)
        {
            // 429: чекаємо довше за звичайний backoff — використовуємо Retry-After від сервера,
            // якщо він більший за подвоєний поточний інтервал, інакше просто подвоюємо інтервал.
            var minimumWait = defaultDelay * 2;
            return apiEx.RetryAfter is { } retryAfter && retryAfter > minimumWait ? retryAfter : minimumWait;
        }

        return defaultDelay;
    }
}

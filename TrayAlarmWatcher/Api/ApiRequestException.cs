using System.Net;

namespace TrayAlarmWatcher.Api;

public sealed class ApiRequestException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public TimeSpan? RetryAfter { get; }

    public ApiRequestException(string message, HttpStatusCode? statusCode, TimeSpan? retryAfter, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        RetryAfter = retryAfter;
    }
}

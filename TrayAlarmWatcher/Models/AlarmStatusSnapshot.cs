namespace TrayAlarmWatcher.Models;

public sealed record AlarmStatusSnapshot(AlarmStatus Status, DateTime CheckedAtLocal, string? ErrorMessage)
{
    public static AlarmStatusSnapshot InitialUnknown() => new(AlarmStatus.Unknown, DateTime.Now, null);
}

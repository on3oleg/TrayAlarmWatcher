namespace TrayAlarmWatcher.Models;

public enum AlarmStatus
{
    Unknown,
    Calm,

    /// <summary>Рівень "Yellow" від API — часткова/підвищена небезпека, ще не повна тривога.</summary>
    Elevated,

    /// <summary>Рівень "Red" від API — повна тривога.</summary>
    Alarm
}

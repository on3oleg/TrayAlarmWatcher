using TrayAlarmWatcher.Configuration;

namespace TrayAlarmWatcher.Services;

public static class FileLogger
{
    private static readonly object SyncRoot = new();

    public static string LogFilePath { get; } = Path.Combine(AppConfig.DirectoryPath, "log.txt");

    public static void LogError(string message)
    {
        lock (SyncRoot)
        {
            try
            {
                Directory.CreateDirectory(AppConfig.DirectoryPath);
                File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR {message}{Environment.NewLine}");
            }
            catch
            {
                // Логування не повинно валити застосунок, якщо диск недоступний чи файл заблокований.
            }
        }
    }
}

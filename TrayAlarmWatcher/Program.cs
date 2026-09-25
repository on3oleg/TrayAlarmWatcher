using TrayAlarmWatcher.Services;

namespace TrayAlarmWatcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Трей-застосунок без вікон не повинен ніколи показувати краш-діалог .NET -
        // будь-який непередбачений виняток логуємо і продовжуємо роботу замість падіння.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            FileLogger.LogError($"Необроблений виняток у UI-потоці: {e.Exception}");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            FileLogger.LogError($"Необроблений виняток поза UI-потоком (IsTerminating={e.IsTerminating}): {e.ExceptionObject}");

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            FileLogger.LogError($"Необроблений виняток у фоновому Task: {e.Exception}");
            e.SetObserved();
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}

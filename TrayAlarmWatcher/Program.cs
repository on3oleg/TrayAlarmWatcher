using System.Runtime.InteropServices;
using TrayAlarmWatcher.Services;

namespace TrayAlarmWatcher;

internal static class Program
{
    [DllImport("shell32.dll", SetLastError = true)]
    private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

    [STAThread]
    private static void Main()
    {
        // Без стабільного AppUserModelID Windows реєструє застосунок під новим
        // автогенерованим ідентифікатором при кожному запуску (NotifyIconGeneratedAumid_*)
        // і на нових білдах Windows 11 balloon-сповіщення NotifyIcon для таких "анонімних"
        // застосунків часто взагалі не показуються і не потрапляють в Action Center.
        SetCurrentProcessExplicitAppUserModelID("TrayAlarmWatcher.AlertsMonitor");

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

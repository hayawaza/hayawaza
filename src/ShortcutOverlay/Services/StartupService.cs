using Microsoft.Win32;

namespace ShortcutOverlay.Services;

public static class StartupService
{
    private const string AppName = "Hayawaza";
    private const string RunKey  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(AppName) != null;
    }

    public static void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key == null) return;
        if (enable)
            key.SetValue(AppName, $"\"{GetExePath()}\"");
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    private static string GetExePath() =>
        Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
}

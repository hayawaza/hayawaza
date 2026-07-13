using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace ShortcutOverlay.Services;

public class ForegroundWatcher
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private static readonly HashSet<string> DefaultTargets =
        new(StringComparer.OrdinalIgnoreCase) { "excel", "powerpnt" };

    private readonly SettingsService _settings;
    private readonly DispatcherTimer _timer;
    private string _lastProcessName = string.Empty;

    /// <summary>前面アプリが変化したとき発火。対象外になった場合は (string.Empty, IntPtr.Zero)。</summary>
    public event Action<string, IntPtr>? AppChanged;

    public ForegroundWatcher(SettingsService settings)
    {
        _settings = settings;
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(settings.PollingIntervalMs)
        };
        _timer.Tick += OnTick;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        var expectedInterval = TimeSpan.FromMilliseconds(_settings.PollingIntervalMs);
        if (_timer.Interval != expectedInterval)
            _timer.Interval = expectedInterval;

        var hwnd = GetForegroundWindow();
        var processName = GetProcessName(hwnd);

        if (processName == _lastProcessName) return;
        _lastProcessName = processName;

        if (IsTarget(processName))
            AppChanged?.Invoke(processName, hwnd);
        else
            AppChanged?.Invoke(string.Empty, IntPtr.Zero);
    }

    private string GetProcessName(IntPtr hwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero) return string.Empty;
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return string.Empty;
            return Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant();
        }
        catch { return string.Empty; }
    }

    private bool IsTarget(string processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;

        var targets = _settings.DebugTargetProcesses is { Count: > 0 } debug
            ? new HashSet<string>(debug, StringComparer.OrdinalIgnoreCase)
            : DefaultTargets;

        return targets.Contains(processName);
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using ShortcutOverlay.Services;

namespace ShortcutOverlay.Services;

/// <summary>
/// PoC-3: GetForegroundWindow を DispatcherTimer で定期ポーリングし、
/// 対象アプリへの切替を AppChanged イベントで通知する。
/// </summary>
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

    /// <summary>前面アプリが変化したとき（または対象外になったとき ""）に発火</summary>
    public event Action<string>? AppChanged;

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
        // ポーリング間隔が設定変更された場合に反映
        var expectedInterval = TimeSpan.FromMilliseconds(_settings.PollingIntervalMs);
        if (_timer.Interval != expectedInterval)
            _timer.Interval = expectedInterval;

        var processName = GetForegroundProcessName();

        if (processName == _lastProcessName) return;
        _lastProcessName = processName;

        AppChanged?.Invoke(IsTarget(processName) ? processName : string.Empty);
    }

    private string GetForegroundProcessName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return string.Empty;

            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == 0) return string.Empty;

            var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName.ToLowerInvariant();
        }
        catch
        {
            return string.Empty;
        }
    }

    private bool IsTarget(string processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;

        // デバッグ用プロセス差し替え（SPEC.md 6.1）
        var targets = _settings.DebugTargetProcesses is { Count: > 0 } debug
            ? new HashSet<string>(debug, StringComparer.OrdinalIgnoreCase)
            : DefaultTargets;

        return targets.Contains(processName);
    }
}

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ShortcutOverlay.Models;
using ShortcutOverlay.Services;

namespace ShortcutOverlay;

public partial class MainWindow : Window
{
    // ────────────────────────────────────
    // Win32 P/Invoke
    // ────────────────────────────────────
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

    // 低レベルキーボードフック
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN   = 0x0100;
    private const int WM_KEYUP     = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP   = 0x0105;
    private const uint VK_LSHIFT = 0xA0; // 左Shift
    private const uint VK_RSHIFT = 0xA1; // 右Shift

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const uint WDA_EXCLUDEFROMCAPTURE   = 0x11;

    // GetAsyncKeyState: 最上位ビットが立っていれば押下中
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public MonRect rcMonitor;
        public MonRect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonRect { public int Left, Top, Right, Bottom; }

    // カテゴリ直接ナビ用 hotkey (Ctrl+Alt+, / .)
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_NEXT = 9002;
    private const int HOTKEY_ID_PREV = 9003;
    private const uint MOD_CTRL_ALT = 3; // MOD_ALT(1) | MOD_CONTROL(2)

    // ────────────────────────────────────
    // フィールド
    // ────────────────────────────────────
    private readonly SettingsService _settings;
    private readonly ShortcutDataService _shortcutData;
    private readonly ForegroundWatcher _watcher;

    private string _currentApp = string.Empty;
    private bool _isSettingsPanelOpen = false;

    private List<ShortcutViewModel> _allCurrentShortcuts = new();
    private List<string> _categories = new();
    private int _currentCategoryIndex = 0;

    private double _monitorWorkLeftDip = 0;
    private double _monitorWorkTopDip = 0;

    // キーポーリング (WH_KEYBOARD_LL の代替 — EDR 回避)
    // volatile: バックグラウンドスレッドからの書き込みをUIスレッドに即時可視化
    private System.Threading.Timer? _keyPollTimer;
    private volatile bool _peekKeyDown   = false;
    private volatile bool _leftShiftPrev = false;

    public MainWindow(SettingsService settings)
    {
        _settings = settings;
        _shortcutData = new ShortcutDataService();
        _watcher = new ForegroundWatcher(_settings);

        InitializeComponent();
        ApplySettings();

        _watcher.AppChanged += OnAppChanged;
        _watcher.Start();

        Left = _settings.OverlayLeft;
        Top = _settings.OverlayTop;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    // ────────────────────────────────────
    // 初期化 / 終了
    // ────────────────────────────────────
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnableClickThrough();

        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

        RegisterHotKey(hwnd, HOTKEY_ID_NEXT, MOD_CTRL_ALT, 0xBE); // Ctrl+Alt+.
        RegisterHotKey(hwnd, HOTKEY_ID_PREV, MOD_CTRL_ALT, 0xBC); // Ctrl+Alt+,

        StartKeyPoller();
        Visibility = Visibility.Hidden;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _watcher.Stop();
        _keyPollTimer?.Dispose();

        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, HOTKEY_ID_NEXT);
        UnregisterHotKey(hwnd, HOTKEY_ID_PREV);

        // 位置はアンカー設定で管理するため終了時の座標保存は不要
    }

    private void ApplySettings()
    {
        ApplyTheme();
        Opacity = _settings.OverlayOpacity;
        var scale = _settings.OverlayScale;
        OverlayPanel.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
    }

    private void ApplyTheme()
    {
        var name = string.IsNullOrEmpty(_settings.Theme) ? "dark" : _settings.Theme;
        var capitalized = char.ToUpper(name[0]) + name.Substring(1);
        var uri = new Uri($"pack://application:,,,/Themes/{capitalized}Theme.xaml");

        var app = System.Windows.Application.Current;
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);
        if (existing != null)
            app.Resources.MergedDictionaries.Remove(existing);

        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
    }

    // ────────────────────────────────────
    // クリックスルー
    // ────────────────────────────────────
    private void EnableClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
    }

    private void DisableClickThrough()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
    }

    // ────────────────────────────────────
    // マルチモニター
    // ────────────────────────────────────
    private void MoveToSameMonitorAs(IntPtr targetHwnd)
    {
        if (targetHwnd == IntPtr.Zero) return;

        var hMonitor = MonitorFromWindow(targetHwnd, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hMonitor, ref info)) return;

        var source = PresentationSource.FromVisual(this);
        double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        double wl = info.rcWork.Left   / dpiX;
        double wt = info.rcWork.Top    / dpiY;
        double ww = (info.rcWork.Right  - info.rcWork.Left) / dpiX;
        double wh = (info.rcWork.Bottom - info.rcWork.Top)  / dpiY;

        _monitorWorkLeftDip = wl;
        _monitorWorkTopDip  = wt;

        double m  = _settings.OverlayMargin;
        double ow = ActualWidth  > 0 ? ActualWidth  : 280;
        double oh = ActualHeight > 0 ? ActualHeight : 400;

        (Left, Top) = _settings.OverlayAnchor switch
        {
            "top-right"    => (wl + ww - ow - m, wt + m),
            "bottom-left"  => (wl + m,            wt + wh - oh - m),
            "bottom-right" => (wl + ww - ow - m,  wt + wh - oh - m),
            _              => (wl + m,             wt + m),  // top-left (default)
        };
    }

    // ────────────────────────────────────
    // 前面プロセス変化
    // ────────────────────────────────────
    private void OnAppChanged(string processName, IntPtr hwnd)
    {
        Dispatcher.Invoke(() =>
        {
            var isNewApp = processName != _currentApp;
            _currentApp = processName;

            if (!string.IsNullOrEmpty(processName))
                MoveToSameMonitorAs(hwnd);

            UpdateOverlay(processName, resetCategory: isNewApp);
            DebugLabel.Text = $"proc: {processName}";
        });
    }

    private void UpdateOverlay(string processName, bool resetCategory = true)
    {
        var shortcuts = _shortcutData.GetVisible(processName, _settings.HiddenShortcuts);

        if (shortcuts.Count == 0)
        {
            if (_settings.DisplayMode == "always")
                Visibility = Visibility.Hidden;
            return;
        }

        _allCurrentShortcuts = shortcuts;
        _categories = shortcuts.Select(s => s.Category).Distinct().ToList();

        if (resetCategory)
            _currentCategoryIndex = 0;
        else
            _currentCategoryIndex = Math.Clamp(_currentCategoryIndex, 0, _categories.Count - 1);

        AppNameLabel.Text = processName switch
        {
            "excel" => "Excel",
            "powerpnt" => "PowerPoint",
            _ => processName
        };

        ShowCurrentCategory();

        if (_settings.DisplayMode == "always")
            Visibility = Visibility.Visible;
    }

    // ────────────────────────────────────
    // カテゴリページング
    // ────────────────────────────────────
    private void ShowCurrentCategory()
    {
        if (_categories.Count == 0) return;

        var cat = _categories[_currentCategoryIndex];
        CategoryLabel.Text = cat;
        CategoryPageLabel.Text = $"({_currentCategoryIndex + 1}/{_categories.Count})";

        var items = _allCurrentShortcuts.Where(s => s.Category == cat).ToList();
        ShortcutList.ItemsSource = BuildDisplayItems(items);
    }

    // グループ境界にミニ区切りを挿入（combo群↔Alt系群、Alt前3キーが変わる境界）
    private static List<object> BuildDisplayItems(List<ShortcutViewModel> items)
    {
        var result = new List<object>();
        string? lastKey = null;

        foreach (var item in items)
        {
            // Alt シーケンスは第3キーまでグループキーに使う (Alt|H|B, Alt|H|A, Alt|J|D など)
            var key = item.Keys.Count > 0 && item.Keys[0] == "Alt"
                ? string.Join("|", item.Keys.Take(Math.Min(3, item.Keys.Count)))
                : "combo";

            if (lastKey != null && key != lastKey)
                result.Add(new SeparatorItem());

            result.Add(item);
            lastKey = key;
        }
        return result;
    }

    private void NavigateCategory(int delta)
    {
        if (_categories.Count == 0 || Visibility != Visibility.Visible) return;
        _currentCategoryIndex = (_currentCategoryIndex + delta + _categories.Count) % _categories.Count;
        ShowCurrentCategory();
    }

    // ────────────────────────────────────
    // 表示 / 非表示
    // ────────────────────────────────────
    private void ShowOverlay()
    {
        if (string.IsNullOrEmpty(_currentApp)) return;
        UpdateOverlay(_currentApp, resetCategory: false);
        Visibility = Visibility.Visible;
    }

    private void HideOverlay()
    {
        Visibility = Visibility.Hidden;
    }

    // ────────────────────────────────────
    // キーポーリング（WH_KEYBOARD_LL の代替 — EDR フレンドリー）
    // ────────────────────────────────────
    private void StartKeyPoller()
    {
        // バックグラウンドスレッドでポーリング → UIスレッド・DWMに負荷をかけない
        _keyPollTimer = new System.Threading.Timer(_ => PollKeys(), null, 0, 30);
    }

    private void PollKeys()
    {
        bool isDown = (GetAsyncKeyState(_settings.HotkeyVk) & 0x8000) != 0;

        if (isDown && !_peekKeyDown)
        {
            _peekKeyDown = true;
            Dispatcher.BeginInvoke(ShowOverlay);
        }
        else if (!isDown && _peekKeyDown)
        {
            _peekKeyDown = false;
            if (_settings.DisplayMode != "always")
                Dispatcher.BeginInvoke(HideOverlay);
        }

        // 左Shift 同時押し: 立ち上がりのみ検出（volatile により他スレッドの書き込みが即時可視）
        bool leftShiftNow = _peekKeyDown && (GetAsyncKeyState((int)VK_LSHIFT) & 0x8000) != 0;
        if (leftShiftNow && !_leftShiftPrev)
            Dispatcher.BeginInvoke(() => NavigateCategory(+1));
        _leftShiftPrev = leftShiftNow;
    }

    // ────────────────────────────────────
    // WndProc（カテゴリ直接ナビ）
    // ────────────────────────────────────
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if      (id == HOTKEY_ID_NEXT) { NavigateCategory(+1); handled = true; }
            else if (id == HOTKEY_ID_PREV) { NavigateCategory(-1); handled = true; }
        }
        return IntPtr.Zero;
    }

    // ────────────────────────────────────
    // 設定パネル
    // ────────────────────────────────────
    public void OpenSettingsPanel()
    {
        if (_isSettingsPanelOpen) return;
        _isSettingsPanelOpen = true;
        DisableClickThrough();

        var settingsWin = new Views.SettingsWindow(_settings, _shortcutData);
        // Owner を設定しない: MainWindow は AllowsTransparency=True のため
        // Owner にすると DWM が SettingsWindow も透明描画パスで処理し
        // Background="#12121C" が効かなくなる（文字色が見えない原因）
        settingsWin.Closed += (_, _) =>
        {
            _isSettingsPanelOpen = false;
            EnableClickThrough();
            ApplySettings();

            if (!string.IsNullOrEmpty(_currentApp))
                UpdateOverlay(_currentApp, resetCategory: false);
        };
        settingsWin.Show();
    }
}

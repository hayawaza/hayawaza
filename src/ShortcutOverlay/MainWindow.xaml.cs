using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Threading;
using ShortcutOverlay.Models;
using ShortcutOverlay.Services;

namespace ShortcutOverlay;

public partial class MainWindow : Window
{
    // ────────────────────────────────────
    // Win32 P/Invoke (PoC-2, PoC-3)
    // ────────────────────────────────────
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // ホットキー登録 (MVP-5)
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9001;
    private const int WM_HOTKEY = 0x0312;

    // ────────────────────────────────────
    // フィールド
    // ────────────────────────────────────
    private readonly SettingsService _settings;
    private readonly ShortcutDataService _shortcutData;
    private readonly ForegroundWatcher _watcher;
    private readonly ObservableCollection<ShortcutViewModel> _currentShortcuts = new();

    private bool _isSettingsPanelOpen = false;
    private string _currentApp = string.Empty;

    public MainWindow(SettingsService settings)
    {
        _settings = settings;
        _shortcutData = new ShortcutDataService();
        _watcher = new ForegroundWatcher(_settings);

        InitializeComponent();
        ApplySettings();

        _watcher.AppChanged += OnAppChanged;
        _watcher.Start();

        // PoC-1: 初期位置
        Left = _settings.OverlayLeft;
        Top = _settings.OverlayTop;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    // ────────────────────────────────────
    // 初期化
    // ────────────────────────────────────
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // PoC-2: 起動直後にクリックスルーを有効化
        EnableClickThrough();

        // MVP-5: ホットキー登録
        var hwnd = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        RegisterOverlayHotKey(hwnd);

        // 初期状態は非表示（対象アプリが前面になるまで）
        Visibility = Visibility.Hidden;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _watcher.Stop();
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, HOTKEY_ID);
        _settings.OverlayLeft = Left;
        _settings.OverlayTop = Top;
    }

    // ────────────────────────────────────
    // PoC-1: 透過・最前面・枠なし
    //   → XAML で設定済み (Topmost/AllowsTransparency/WindowStyle=None)
    //   → 透過率は OpacityProperty で制御
    // ────────────────────────────────────
    private void ApplySettings()
    {
        Opacity = _settings.OverlayOpacity;
        var scale = _settings.OverlayScale;
        OverlayPanel.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
    }

    // ────────────────────────────────────
    // PoC-2: クリックスルー動的切替
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
    // PoC-3: 前面プロセス変化時コールバック
    // ────────────────────────────────────
    private void OnAppChanged(string processName)
    {
        Dispatcher.Invoke(() =>
        {
            _currentApp = processName;
            UpdateOverlay(processName);

            // デバッグラベル
            DebugLabel.Text = $"proc: {processName}";
        });
    }

    private void UpdateOverlay(string processName)
    {
        var shortcuts = _shortcutData.GetVisible(processName, _settings.HiddenShortcuts);

        if (shortcuts.Count == 0)
        {
            if (_settings.DisplayMode == "always")
                Visibility = Visibility.Hidden;
            return;
        }

        // カテゴリグループ表示
        var view = CollectionViewSource.GetDefaultView(shortcuts);
        view.GroupDescriptions.Clear();
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ShortcutViewModel.Category)));
        ShortcutList.ItemsSource = view;

        AppNameLabel.Text = processName switch
        {
            "excel" => "Excel",
            "powerpnt" => "PowerPoint",
            _ => processName
        };

        if (_settings.DisplayMode == "always")
            Visibility = Visibility.Visible;
    }

    // ────────────────────────────────────
    // MVP-5: ホットキー (RegisterHotKey)
    // ────────────────────────────────────
    private void RegisterOverlayHotKey(IntPtr hwnd)
    {
        if (_settings.DisplayMode != "hotkey") return;
        RegisterHotKey(hwnd, HOTKEY_ID, (uint)_settings.HotkeyModifiers, (uint)_settings.HotkeyVk);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleHotkeyVisibility();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void ToggleHotkeyVisibility()
    {
        if (Visibility == Visibility.Visible)
            Visibility = Visibility.Hidden;
        else if (!string.IsNullOrEmpty(_currentApp))
            UpdateOverlay(_currentApp);
    }

    // ────────────────────────────────────
    // 設定パネル開閉 (トレイから呼ばれる)
    // ────────────────────────────────────
    public void OpenSettingsPanel()
    {
        if (_isSettingsPanelOpen) return;
        _isSettingsPanelOpen = true;
        DisableClickThrough();

        var settingsWin = new Views.SettingsWindow(_settings, _shortcutData);
        settingsWin.Owner = this;
        settingsWin.Closed += (_, _) =>
        {
            _isSettingsPanelOpen = false;
            EnableClickThrough();
            ApplySettings();

            // ホットキー再登録
            var hwnd = new WindowInteropHelper(this).Handle;
            UnregisterHotKey(hwnd, HOTKEY_ID);
            RegisterOverlayHotKey(hwnd);

            // 表示更新
            if (!string.IsNullOrEmpty(_currentApp))
                UpdateOverlay(_currentApp);
        };
        settingsWin.Show();
    }
}

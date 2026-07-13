using ShortcutOverlay.Services;

namespace ShortcutOverlay;

public partial class App : System.Windows.Application
{
    private static Mutex? _instanceMutex;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private SettingsService? _settings;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        // 多重起動防止
        _instanceMutex = new Mutex(true, "Hayawaza_SingleInstance_v2", out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _settings = new SettingsService();
        _settings.Load();

        if (_settings.IsFirstRun)
        {
            _settings.IsFirstRun = false;
            _settings.Save();
            var welcome = new Views.WelcomeWindow();
            welcome.ShowDialog();
        }

        _mainWindow = new MainWindow(_settings);

        // ウィンドウアイコンをファイルから設定
        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");
            if (System.IO.File.Exists(iconPath))
                _mainWindow.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(
                    new Uri(iconPath, UriKind.Absolute));
        }
        catch { }

        InitTrayIcon();

        _mainWindow.Show();
    }

    private void InitTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "Hayawaza",
            Visible = true,
        };

        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
            if (System.IO.File.Exists(iconPath))
                _trayIcon.Icon = new System.Drawing.Icon(iconPath);
            else
                _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }
        catch
        {
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("設定を開く", null, (_, _) => OpenSettings());
        menu.Items.Add("-");
        menu.Items.Add("終了", null, (_, _) => ExitApp());

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.DoubleClick += (_, _) => OpenSettings();
    }

    private void OpenSettings()
    {
        _mainWindow?.OpenSettingsPanel();
    }

    private void ExitApp()
    {
        _trayIcon?.Dispose();
        _mainWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _settings?.Save();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}

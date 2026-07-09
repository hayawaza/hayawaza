using System.Windows;
using ShortcutOverlay.Services;

namespace ShortcutOverlay;

public partial class App : Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private SettingsService? _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = new SettingsService();
        _settings.Load();

        _mainWindow = new MainWindow(_settings);

        InitTrayIcon();

        _mainWindow.Show();
    }

    private void InitTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "ShortcutOverlay",
            Visible = true,
        };

        // アイコンがない場合は SystemIcons を流用
        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "icon.ico");
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

    protected override void OnExit(ExitEventArgs e)
    {
        _settings?.Save();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}

using System.IO;
using System.Text.Json;
using ShortcutOverlay.Models;

namespace ShortcutOverlay.Services;

/// <summary>
/// %LOCALAPPDATA%\ShortcutOverlay\settings.json への設定保存/復元。
/// AppSettings のプロパティをそのまま公開し、各 UI 層が直接読み書きする。
/// </summary>
public class SettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hayawaza");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private AppSettings _data = new();

    // ── プロパティ（AppSettings をフラットに公開） ──────────────────────

    public string DisplayMode
    {
        get => _data.DisplayMode;
        set => _data.DisplayMode = value;
    }

    public string Theme
    {
        get => _data.Theme;
        set => _data.Theme = value;
    }

    public int HotkeyModifiers
    {
        get => _data.HotkeyModifiers;
        set => _data.HotkeyModifiers = value;
    }

    public int HotkeyVk
    {
        get => _data.HotkeyVk;
        set => _data.HotkeyVk = value;
    }

    public double OverlayOpacity
    {
        get => _data.OverlayOpacity;
        set => _data.OverlayOpacity = Math.Clamp(value, 0.1, 1.0);
    }

    public double OverlayScale
    {
        get => _data.OverlayScale;
        set => _data.OverlayScale = Math.Clamp(value, 0.5, 2.0);
    }

    public string OverlayAnchor
    {
        get => _data.OverlayAnchor;
        set => _data.OverlayAnchor = value;
    }

    public double OverlayMargin
    {
        get => _data.OverlayMargin;
        set => _data.OverlayMargin = Math.Max(0, value);
    }

    public double OverlayLeft
    {
        get => _data.OverlayLeft;
        set => _data.OverlayLeft = value;
    }

    public double OverlayTop
    {
        get => _data.OverlayTop;
        set => _data.OverlayTop = value;
    }

    public List<string> HiddenShortcuts => _data.HiddenShortcuts;

    public int PollingIntervalMs
    {
        get => _data.PollingIntervalMs;
        set => _data.PollingIntervalMs = Math.Max(100, value);
    }

    public List<string> DebugTargetProcesses => _data.DebugTargetProcesses;

    // ── ロード / セーブ ────────────────────────────────────────────────

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            _data = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            _data = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(_data, options));
        }
        catch
        {
            // 保存失敗は無視（次回起動時にデフォルト値で起動）
        }
    }

    /// <summary>ショートカットの非表示状態をトグル</summary>
    public void ToggleHidden(string id)
    {
        if (_data.HiddenShortcuts.Contains(id))
            _data.HiddenShortcuts.Remove(id);
        else
            _data.HiddenShortcuts.Add(id);
    }
}

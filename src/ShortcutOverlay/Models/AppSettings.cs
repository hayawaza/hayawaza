namespace ShortcutOverlay.Models;

/// <summary>settings.json のスキーマ定義（SPEC.md 5.2）</summary>
public class AppSettings
{
    /// <summary>"always" or "hotkey"</summary>
    public string DisplayMode { get; set; } = "always";

    /// <summary>RegisterHotKey の fsModifiers (MOD_SHIFT=4, MOD_CTRL=2, MOD_ALT=1)</summary>
    public int HotkeyModifiers { get; set; } = 6; // Ctrl+Shift

    /// <summary>仮想キーコード (既定: S = 0x53)</summary>
    public int HotkeyVk { get; set; } = 0x53;

    /// <summary>オーバーレイ不透明度 0.0–1.0</summary>
    public double OverlayOpacity { get; set; } = 0.85;

    /// <summary>スケール倍率</summary>
    public double OverlayScale { get; set; } = 1.0;

    /// <summary>画面左端からのピクセル位置</summary>
    public double OverlayLeft { get; set; } = 50;

    /// <summary>画面上端からのピクセル位置</summary>
    public double OverlayTop { get; set; } = 50;

    /// <summary>非表示ショートカットの id リスト</summary>
    public List<string> HiddenShortcuts { get; set; } = new();

    /// <summary>ポーリング間隔 ms（既定 300）</summary>
    public int PollingIntervalMs { get; set; } = 300;

    /// <summary>デバッグ用プロセス差し替え。空なら excel/powerpnt を使用</summary>
    public List<string> DebugTargetProcesses { get; set; } = new();
}

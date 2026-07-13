namespace ShortcutOverlay.Models;

/// <summary>settings.json のスキーマ定義（SPEC.md 5.2）</summary>
public class AppSettings
{
    /// <summary>"always" or "hotkey"</summary>
    public string DisplayMode { get; set; } = "hotkey";

    /// <summary>"dark" or "white"</summary>
    public string Theme { get; set; } = "dark";

    /// <summary>RegisterHotKey の fsModifiers (MOD_SHIFT=4, MOD_CTRL=2, MOD_ALT=1) — 単キーモードは 0</summary>
    public int HotkeyModifiers { get; set; } = 0;

    /// <summary>仮想キーコード (既定: 右Shift = 0xA1)</summary>
    public int HotkeyVk { get; set; } = 0xA1;

    /// <summary>オーバーレイ不透明度 0.0–1.0</summary>
    public double OverlayOpacity { get; set; } = 0.97;

    /// <summary>スケール倍率</summary>
    public double OverlayScale { get; set; } = 1.0;

    /// <summary>配置コーナー: "top-left" / "top-right" / "bottom-left" / "bottom-right"</summary>
    public string OverlayAnchor { get; set; } = "top-left";

    /// <summary>コーナーからのオフセット (DIP px)</summary>
    public double OverlayMargin { get; set; } = 40;

    /// <summary>旧 X 座標 (互換用。OverlayAnchor が空の場合のみ参照)</summary>
    public double OverlayLeft { get; set; } = 50;

    /// <summary>旧 Y 座標 (互換用)</summary>
    public double OverlayTop { get; set; } = 50;

    /// <summary>非表示ショートカットの id リスト</summary>
    public List<string> HiddenShortcuts { get; set; } = new();

    /// <summary>ポーリング間隔 ms（既定 300）</summary>
    public int PollingIntervalMs { get; set; } = 300;

    /// <summary>デバッグ用プロセス差し替え。空なら excel/powerpnt を使用</summary>
    public List<string> DebugTargetProcesses { get; set; } = new();

    /// <summary>初回起動フラグ。ウェルカム画面表示後に false にする</summary>
    public bool IsFirstRun { get; set; } = true;
}

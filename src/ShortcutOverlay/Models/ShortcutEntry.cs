namespace ShortcutOverlay.Models;

/// <summary>shortcuts.json の1エントリに対応するデータモデル</summary>
public class ShortcutEntry
{
    public string Id { get; set; } = string.Empty;
    public string App { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<string> Keys { get; set; } = new();

    /// <summary>"combo" (同時押し) or "sequence" (順押し)</summary>
    public string KeyType { get; set; } = "combo";

    public bool DefaultVisible { get; set; } = true;
}

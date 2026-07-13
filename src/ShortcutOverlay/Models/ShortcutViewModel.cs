using System.ComponentModel;

namespace ShortcutOverlay.Models;

/// <summary>オーバーレイ表示用 ViewModel (INotifyPropertyChanged)</summary>
public class ShortcutViewModel : INotifyPropertyChanged
{
    private bool _isVisible = true;

    public string Id { get; init; } = string.Empty;
    public string App { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;

    /// <summary>生キーリスト（共通プレフィクス検出用）</summary>
    public List<string> Keys { get; init; } = new();

    /// <summary>表示用キー文字列: combo="Ctrl+Home" / sequence="Alt→JDAAB"</summary>
    public string KeysDisplay { get; init; } = string.Empty;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static ShortcutViewModel From(ShortcutEntry entry)
    {
        var display = entry.KeyType == "sequence"
            ? FormatSequence(entry.Keys)
            : string.Join("+", entry.Keys);

        return new ShortcutViewModel
        {
            Id = entry.Id,
            App = entry.App,
            Category = entry.Category,
            Label = entry.Label,
            Keys = entry.Keys,
            KeysDisplay = display,
            IsVisible = entry.DefaultVisible,
        };
    }

    // 各キーをスペース区切りで表示（例: "Alt J D A A L"）
    private static string FormatSequence(List<string> keys)
    {
        if (keys.Count == 0) return string.Empty;
        return string.Join(" ", keys);
    }
}

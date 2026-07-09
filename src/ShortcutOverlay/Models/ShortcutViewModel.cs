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

    /// <summary>表示用キー文字列: combo="Ctrl+Home" / sequence="Alt → H → 1"</summary>
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
            ? string.Join(" → ", entry.Keys)
            : string.Join("+", entry.Keys);

        return new ShortcutViewModel
        {
            Id = entry.Id,
            App = entry.App,
            Category = entry.Category,
            Label = entry.Label,
            KeysDisplay = display,
            IsVisible = entry.DefaultVisible,
        };
    }
}

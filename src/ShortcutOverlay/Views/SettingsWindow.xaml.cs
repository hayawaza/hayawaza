using System.Windows;
using System.Windows.Controls;
using ShortcutOverlay.Services;

namespace ShortcutOverlay.Views;

/// <summary>設定 UI ウィンドウ（MVP 7-9）</summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly ShortcutDataService _shortcutData;

    // ホットキー選択肢
    private static readonly (string Label, int Modifiers, int Vk)[] HotkeyOptions =
    [
        ("Ctrl+Shift+S", 6, 0x53),
        ("Ctrl+Shift+K", 6, 0x4B),
        ("Ctrl+Shift+H", 6, 0x48),
        ("Ctrl+Alt+S",   5, 0x53),
        ("Ctrl+Alt+K",   5, 0x4B),
    ];

    public SettingsWindow(SettingsService settings, ShortcutDataService shortcutData)
    {
        _settings = settings;
        _shortcutData = shortcutData;
        InitializeComponent();
        LoadValues();
    }

    private void LoadValues()
    {
        // 表示モード
        foreach (ComboBoxItem item in DisplayModeCombo.Items)
            if ((string)item.Tag == _settings.DisplayMode)
                DisplayModeCombo.SelectedItem = item;
        if (DisplayModeCombo.SelectedItem is null)
            DisplayModeCombo.SelectedIndex = 0;
        DisplayModeCombo.SelectionChanged += DisplayModeCombo_SelectionChanged;
        UpdateHotkeyPanelVisibility();

        // ホットキーコンボ
        foreach (var opt in HotkeyOptions)
            HotkeyCombo.Items.Add(new ComboBoxItem { Content = opt.Label, Tag = opt });
        for (int i = 0; i < HotkeyOptions.Length; i++)
        {
            if (HotkeyOptions[i].Modifiers == _settings.HotkeyModifiers &&
                HotkeyOptions[i].Vk == _settings.HotkeyVk)
            {
                HotkeyCombo.SelectedIndex = i;
                break;
            }
        }
        if (HotkeyCombo.SelectedItem is null) HotkeyCombo.SelectedIndex = 0;

        // スライダー
        OpacitySlider.Value = _settings.OverlayOpacity * 100;
        ScaleSlider.Value = _settings.OverlayScale * 100;
        PosLeftSlider.Value = _settings.OverlayLeft;
        PosTopSlider.Value = _settings.OverlayTop;
        PollingSlider.Value = _settings.PollingIntervalMs;

        // ショートカット ON/OFF リスト
        ExcelShortcutList.ItemsSource = BuildCheckItems("excel");
        PptShortcutList.ItemsSource = BuildCheckItems("powerpnt");

        // デバッグプロセス
        DebugProcessBox.Text = string.Join(",", _settings.DebugTargetProcesses);
    }

    private List<ShortcutCheckItem> BuildCheckItems(string app)
    {
        return _shortcutData.GetAll(app).Select(e => new ShortcutCheckItem
        {
            Id = e.Id,
            DisplayText = $"{e.Label}  [{FormatKeys(e.Keys, e.KeyType)}]",
            IsChecked = !_settings.HiddenShortcuts.Contains(e.Id),
        }).ToList();
    }

    private static string FormatKeys(List<string> keys, string keyType) =>
        keyType == "sequence" ? string.Join(" → ", keys) : string.Join("+", keys);

    // ── イベントハンドラ ───────────────────────────────────────────────

    private void DisplayModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateHotkeyPanelVisibility();
    }

    private void UpdateHotkeyPanelVisibility()
    {
        var selected = (DisplayModeCombo.SelectedItem as ComboBoxItem)?.Tag as string;
        HotkeyPanel.Visibility = selected == "hotkey" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValueLabel != null)
            OpacityValueLabel.Text = $"{(int)e.NewValue}%";
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ScaleValueLabel != null)
            ScaleValueLabel.Text = $"{(int)e.NewValue}%";
    }

    private void PosLeftSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PosLeftLabel != null)
            PosLeftLabel.Text = $"{(int)e.NewValue}";
    }

    private void PosTopSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PosTopLabel != null)
            PosTopLabel.Text = $"{(int)e.NewValue}";
    }

    private void PollingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PollingLabel != null)
            PollingLabel.Text = $"{(int)e.NewValue}ms";
    }

    private void ShortcutCheck_Changed(object sender, RoutedEventArgs e)
    {
        // リアルタイム反映はせず、保存時に一括処理
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // 表示モード
        var modeItem = DisplayModeCombo.SelectedItem as ComboBoxItem;
        _settings.DisplayMode = (string?)modeItem?.Tag ?? "always";

        // ホットキー
        if (HotkeyCombo.SelectedItem is ComboBoxItem hkItem &&
            hkItem.Tag is ValueTuple<string, int, int> hkTuple)
        {
            _settings.HotkeyModifiers = hkTuple.Item2;
            _settings.HotkeyVk = hkTuple.Item3;
        }

        // ビジュアル
        _settings.OverlayOpacity = OpacitySlider.Value / 100.0;
        _settings.OverlayScale = ScaleSlider.Value / 100.0;
        _settings.OverlayLeft = PosLeftSlider.Value;
        _settings.OverlayTop = PosTopSlider.Value;

        // ショートカット ON/OFF
        _settings.HiddenShortcuts.Clear();
        foreach (var item in GetAllCheckItems())
            if (!item.IsChecked)
                _settings.HiddenShortcuts.Add(item.Id);

        // デバッグプロセス
        _settings.DebugTargetProcesses.Clear();
        var debugText = DebugProcessBox.Text.Trim();
        if (!string.IsNullOrEmpty(debugText))
            _settings.DebugTargetProcesses.AddRange(
                debugText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        // ポーリング間隔
        _settings.PollingIntervalMs = (int)PollingSlider.Value;

        _settings.Save();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private IEnumerable<ShortcutCheckItem> GetAllCheckItems()
    {
        foreach (ShortcutCheckItem item in (ExcelShortcutList.ItemsSource ?? Array.Empty<ShortcutCheckItem>()))
            yield return item;
        foreach (ShortcutCheckItem item in (PptShortcutList.ItemsSource ?? Array.Empty<ShortcutCheckItem>()))
            yield return item;
    }
}

/// <summary>設定UIのチェックボックス行 ViewModel</summary>
public class ShortcutCheckItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public bool IsChecked { get; set; } = true;
}

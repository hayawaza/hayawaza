using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using ShortcutOverlay.Models;
using ShortcutOverlay.Services;
using WpfButton    = System.Windows.Controls.Button;
using WpfCheckBox  = System.Windows.Controls.CheckBox;
using WpfMsgBox    = System.Windows.MessageBox;
using WpfColor     = System.Windows.Media.Color;
using WpfBrushes   = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace ShortcutOverlay.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly ShortcutDataService _shortcutData;
    private readonly Dictionary<string, List<CategoryGroupVm>> _groupVmsByApp = new();

    private static readonly (string Label, int Modifiers, int Vk)[] HotkeyOptions =
    [
        ("右Shift（プレス中表示）",        0, 0xA1),  // VK_RSHIFT
        ("右Alt（プレス中表示）",          0, 0xA5),  // VK_RMENU
        ("右Ctrl（プレス中表示）",         0, 0xA3),  // VK_RCONTROL
        ("Scroll Lock（プレス中表示）",    0, 0x91),  // VK_SCROLL
        ("Pause/Break（プレス中表示）",    0, 0x13),  // VK_PAUSE
        ("F12（プレス中表示）",            0, 0x7B),  // VK_F12
    ];

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Windows 11 の Mica/アクリルバックドロップを無効化して背景色を確実に反映させる
        var hwnd = new WindowInteropHelper(this).Handle;
        int noBackdrop = 1; // DWMSBT_NONE
        DwmSetWindowAttribute(hwnd, 38, ref noBackdrop, sizeof(int));
    }

    public SettingsWindow(SettingsService settings, ShortcutDataService shortcutData)
    {
        _settings     = settings;
        _shortcutData = shortcutData;
        InitializeComponent();
        // Loaded 後に初期化することでウィンドウが先に表示され "(応答なし)" を回避
        Loaded += (_, _) => LoadValues();
    }

    // ── 初期化 ────────────────────────────────────────────────────────────

    private void LoadValues()
    {
        // 表示モード
        foreach (ComboBoxItem item in DisplayModeCombo.Items)
            if ((string)item.Tag == _settings.DisplayMode)
                DisplayModeCombo.SelectedItem = item;
        if (DisplayModeCombo.SelectedItem is null) DisplayModeCombo.SelectedIndex = 0;
        DisplayModeCombo.SelectionChanged += DisplayModeCombo_SelectionChanged;
        UpdateHotkeyPanelVisibility();

        // ホットキー
        foreach (var opt in HotkeyOptions)
            HotkeyCombo.Items.Add(new ComboBoxItem { Content = opt.Label, Tag = opt });
        for (int i = 0; i < HotkeyOptions.Length; i++)
        {
            if (HotkeyOptions[i].Modifiers == _settings.HotkeyModifiers &&
                HotkeyOptions[i].Vk == _settings.HotkeyVk)
            { HotkeyCombo.SelectedIndex = i; break; }
        }
        if (HotkeyCombo.SelectedItem is null) HotkeyCombo.SelectedIndex = 0;

        // テーマ
        foreach (ComboBoxItem item in ThemeCombo.Items)
            if ((string)item.Tag == _settings.Theme)
            { ThemeCombo.SelectedItem = item; break; }
        if (ThemeCombo.SelectedItem is null) ThemeCombo.SelectedIndex = 0;

        // スライダー
        OpacitySlider.Value = _settings.OverlayOpacity * 100;
        ScaleSlider.Value   = _settings.OverlayScale   * 100;
        MarginSlider.Value  = _settings.OverlayMargin;
        PollingSlider.Value = _settings.PollingIntervalMs;

        // コーナー選択
        switch (_settings.OverlayAnchor)
        {
            case "top-right":    AnchorTopRight.IsChecked    = true; break;
            case "bottom-left":  AnchorBottomLeft.IsChecked  = true; break;
            case "bottom-right": AnchorBottomRight.IsChecked = true; break;
            default:             AnchorTopLeft.IsChecked     = true; break;
        }

        // ショートカットグループ（code-behind で構築）
        BuildGroupUI(ExcelGroupsPanel, "excel");
        BuildGroupUI(PptGroupsPanel, "powerpnt");

        // 追加コンボのカテゴリ候補
        RefreshAddCategories("excel");
        RefreshAddCategories("powerpnt");

        // デバッグ
        DebugProcessBox.Text = string.Join(",", _settings.DebugTargetProcesses);
    }

    // ── ショートカットグループUI構築（DataTemplate 不使用） ────────────────

    private void BuildGroupUI(StackPanel host, string app)
    {
        host.Children.Clear();
        var groups = BuildCategoryGroups(app);
        _groupVmsByApp[app] = groups;

        foreach (var group in groups)
        {
            var card = new Border
            {
                Background   = new SolidColorBrush(WpfColor.FromRgb(0x1A, 0x1A, 0x28)),
                CornerRadius = new CornerRadius(6),
                Margin       = new Thickness(0, 2, 0, 2),
            };

            var cardStack = new StackPanel();

            // ── カテゴリヘッダー ──
            var hGrid = new Grid { Margin = new Thickness(12, 8, 12, 8) };
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var catCb = new WpfCheckBox
            {
                IsChecked                = group.IsGroupChecked,
                Foreground               = new SolidColorBrush(WpfColor.FromRgb(0xE8, 0xE8, 0xF4)),
                VerticalAlignment        = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(catCb, 0);

            var catText = new TextBlock
            {
                Text              = group.Category,
                Foreground        = new SolidColorBrush(WpfColor.FromRgb(0xE8, 0xE8, 0xF4)),
                FontWeight        = FontWeights.SemiBold,
                FontSize          = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(6, 0, 0, 0),
            };
            Grid.SetColumn(catText, 1);

            var countText = new TextBlock
            {
                Text              = group.CountLabel,
                Foreground        = new SolidColorBrush(WpfColor.FromRgb(0x38, 0x38, 0x58)),
                FontSize          = 10,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(countText, 2);

            hGrid.Children.Add(catCb);
            hGrid.Children.Add(catText);
            hGrid.Children.Add(countText);
            cardStack.Children.Add(hGrid);

            // ── アイテム一覧 ──
            var itemsStack  = new StackPanel { Margin = new Thickness(24, 0, 12, 10) };
            var itemCbList  = new List<WpfCheckBox>();

            foreach (var item in group.Items)
            {
                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                if (item.IsUserDefined)
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var itemCb = new WpfCheckBox
                {
                    IsChecked         = item.IsVisible,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                itemCbList.Add(itemCb);
                Grid.SetColumn(itemCb, 0);

                var capturedItem  = item;
                var capturedCatCb = catCb;
                var capturedGroup = group;
                itemCb.Checked   += (_, _) => { capturedItem.IsVisible = true;  SyncCatCb(capturedCatCb, capturedGroup); };
                itemCb.Unchecked += (_, _) => { capturedItem.IsVisible = false; SyncCatCb(capturedCatCb, capturedGroup); };

                var labelTb = new TextBlock
                {
                    Text              = item.Label,
                    Foreground        = new SolidColorBrush(WpfColor.FromRgb(0xB8, 0xB8, 0xCC)),
                    FontSize          = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(6, 0, 8, 0),
                    TextTrimming      = TextTrimming.CharacterEllipsis,
                };
                Grid.SetColumn(labelTb, 1);

                var keyBorder = new Border
                {
                    Background        = new SolidColorBrush(WpfColor.FromRgb(0x1E, 0x1E, 0x2E)),
                    CornerRadius      = new CornerRadius(3),
                    Padding           = new Thickness(6, 1, 6, 1),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text       = item.KeysDisplay,
                        Foreground = new SolidColorBrush(WpfColor.FromRgb(0x55, 0x55, 0x88)),
                        FontFamily = new WpfFontFamily("Consolas"),
                        FontSize   = 10,
                    }
                };
                Grid.SetColumn(keyBorder, 2);

                row.Children.Add(itemCb);
                row.Children.Add(labelTb);
                row.Children.Add(keyBorder);

                if (item.IsUserDefined)
                {
                    var capturedId    = item.Id;
                    var capturedLabel = item.Label;
                    var capturedApp   = app;

                    var delBtn = new WpfButton
                    {
                        Content           = "×",
                        Width             = 22, Height = 22,
                        Padding           = new Thickness(0),
                        Margin            = new Thickness(6, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Background        = WpfBrushes.Transparent,
                        BorderThickness   = new Thickness(0),
                        Foreground        = new SolidColorBrush(WpfColor.FromRgb(0x44, 0x44, 0x60)),
                        FontSize          = 12,
                        Cursor            = System.Windows.Input.Cursors.Hand,
                    };
                    delBtn.Click += (_, _) => OnDeleteShortcut(capturedId, capturedLabel, capturedApp);
                    Grid.SetColumn(delBtn, 3);
                    row.Children.Add(delBtn);
                }

                itemsStack.Children.Add(row);
            }

            // カテゴリCBクリックで全アイテムを一括 ON/OFF
            var capturedItemCbList = itemCbList;
            var capturedGroupForCat = group;
            catCb.Click += (_, _) =>
            {
                bool v = catCb.IsChecked == true;
                capturedGroupForCat.SetAllItems(v);
                foreach (var cb in capturedItemCbList) cb.IsChecked = v;
            };

            cardStack.Children.Add(itemsStack);
            card.Child = cardStack;
            host.Children.Add(card);
        }
    }

    private static void SyncCatCb(WpfCheckBox catCb, CategoryGroupVm group)
        => catCb.IsChecked = group.Items.Count == 0 || group.Items.All(i => i.IsVisible);

    private List<CategoryGroupVm> BuildCategoryGroups(string app)
    {
        return _shortcutData.GetAll(app)
            .GroupBy(e => e.Category)
            .Select(g =>
            {
                var group = new CategoryGroupVm { App = app, Category = g.Key };
                foreach (var e in g)
                {
                    group.Items.Add(new ShortcutItemVm
                    {
                        Id            = e.Id,
                        Label         = e.Label,
                        KeysDisplay   = FormatKeys(e.Keys, e.KeyType),
                        IsVisible     = !_settings.HiddenShortcuts.Contains(e.Id),
                        IsUserDefined = e.IsUserDefined,
                        ParentGroup   = group,
                    });
                }
                group.RefreshGroupState();
                return group;
            })
            .ToList();
    }

    private void RefreshAddCategories(string app)
    {
        if (app == "excel")
        {
            ExcelAddCategory.Items.Clear();
            foreach (var c in _shortcutData.GetAll("excel").Select(e => e.Category).Distinct())
                ExcelAddCategory.Items.Add(c);
        }
        else
        {
            PptAddCategory.Items.Clear();
            foreach (var c in _shortcutData.GetAll("powerpnt").Select(e => e.Category).Distinct())
                PptAddCategory.Items.Add(c);
        }
    }

    private static string FormatKeys(List<string> keys, string keyType) =>
        keyType == "sequence" ? string.Join(" → ", keys) : string.Join("+", keys);

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.W &&
            (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
        {
            Close();
            e.Handled = true;
        }
    }

    // ── タブ切替 ──────────────────────────────────────────────────────────

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (PageVisual is null) return;
        PageVisual.Visibility    = TabVisual.IsChecked    == true ? Visibility.Visible : Visibility.Collapsed;
        PageShortcuts.Visibility = TabShortcuts.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        PageAdvanced.Visibility  = TabAdvanced.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── 表示モード ────────────────────────────────────────────────────────

    private void DisplayModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => UpdateHotkeyPanelVisibility();

    private void UpdateHotkeyPanelVisibility()
    {
        var sel = (DisplayModeCombo.SelectedItem as ComboBoxItem)?.Tag as string;
        HotkeyPanel.Visibility = sel == "hotkey" ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── スライダー ────────────────────────────────────────────────────────

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (OpacityValueLabel != null) OpacityValueLabel.Text = $"{(int)e.NewValue}%"; }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (ScaleValueLabel != null) ScaleValueLabel.Text = $"{(int)e.NewValue}%"; }

    private void MarginSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (MarginLabel != null) MarginLabel.Text = $"{(int)e.NewValue}px"; }

    private void PollingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (PollingLabel != null) PollingLabel.Text = $"{(int)e.NewValue}ms"; }

    // ── ショートカット追加 ────────────────────────────────────────────────

    private void ShowAddPanel_Click(object sender, RoutedEventArgs e)
    {
        var app = (sender as WpfButton)?.Tag as string;
        if (app == "excel")
        { ExcelAddPanel.Visibility = Visibility.Visible; ExcelAddBtn.Visibility = Visibility.Collapsed; }
        else
        { PptAddPanel.Visibility   = Visibility.Visible; PptAddBtn.Visibility   = Visibility.Collapsed; }
    }

    private void CancelAddPanel_Click(object sender, RoutedEventArgs e)
        => CloseAddPanel((sender as WpfButton)?.Tag as string);

    private void CloseAddPanel(string? app)
    {
        if (app == "excel")
        {
            ExcelAddPanel.Visibility = Visibility.Collapsed;
            ExcelAddBtn.Visibility   = Visibility.Visible;
            ExcelAddLabel.Text = ""; ExcelAddKeys.Text = "";
        }
        else
        {
            PptAddPanel.Visibility = Visibility.Collapsed;
            PptAddBtn.Visibility   = Visibility.Visible;
            PptAddLabel.Text = ""; PptAddKeys.Text = "";
        }
    }

    private void AddShortcut_Click(object sender, RoutedEventArgs e)
    {
        var app     = (sender as WpfButton)?.Tag as string ?? "excel";
        var isExcel = app == "excel";

        var category = (isExcel ? ExcelAddCategory.Text : PptAddCategory.Text).Trim();
        var labelTxt = (isExcel ? ExcelAddLabel.Text    : PptAddLabel.Text).Trim();
        var keysTxt  = (isExcel ? ExcelAddKeys.Text     : PptAddKeys.Text).Trim();
        var keyType  = ((isExcel ? ExcelAddKeyType : PptAddKeyType).SelectedItem as ComboBoxItem)?.Tag as string ?? "combo";

        if (string.IsNullOrEmpty(labelTxt) || string.IsNullOrEmpty(keysTxt))
        {
            WpfMsgBox.Show("ラベルとキーは必須です。", "入力エラー",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var keys = keysTxt.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        _shortcutData.AddUserEntry(new ShortcutEntry
        {
            Id             = $"user-{app}-{Guid.NewGuid():N}"[..24],
            App            = app,
            Category       = string.IsNullOrEmpty(category) ? "カスタム" : category,
            Label          = labelTxt,
            Keys           = keys,
            KeyType        = keyType,
            DefaultVisible = true,
            IsUserDefined  = true,
        });

        CloseAddPanel(app);
        var host = isExcel ? ExcelGroupsPanel : PptGroupsPanel;
        BuildGroupUI(host, app);
        RefreshAddCategories(app);
    }

    private void OnDeleteShortcut(string id, string label, string app)
    {
        if (WpfMsgBox.Show($"「{label}」を削除しますか？",
                "削除確認", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        _shortcutData.RemoveUserEntry(id);
        var host = app == "excel" ? ExcelGroupsPanel : PptGroupsPanel;
        BuildGroupUI(host, app);
        RefreshAddCategories(app);
    }

    // ── 保存 / キャンセル ─────────────────────────────────────────────────

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.DisplayMode = (DisplayModeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "always";

        if (HotkeyCombo.SelectedItem is ComboBoxItem hkItem &&
            hkItem.Tag is ValueTuple<string, int, int> hk)
        {
            _settings.HotkeyModifiers = hk.Item2;
            _settings.HotkeyVk        = hk.Item3;
        }

        _settings.Theme          = (ThemeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "dark";
        _settings.OverlayOpacity = OpacitySlider.Value / 100.0;
        _settings.OverlayScale   = ScaleSlider.Value   / 100.0;
        _settings.OverlayMargin  = MarginSlider.Value;
        _settings.OverlayAnchor  =
            AnchorTopRight.IsChecked    == true ? "top-right"    :
            AnchorBottomLeft.IsChecked  == true ? "bottom-left"  :
            AnchorBottomRight.IsChecked == true ? "bottom-right" : "top-left";

        _settings.HiddenShortcuts.Clear();
        foreach (var (_, groups) in _groupVmsByApp)
            foreach (var g in groups)
                foreach (var item in g.Items)
                    if (!item.IsVisible)
                        _settings.HiddenShortcuts.Add(item.Id);

        _settings.DebugTargetProcesses.Clear();
        var debug = DebugProcessBox.Text.Trim();
        if (!string.IsNullOrEmpty(debug))
            _settings.DebugTargetProcesses.AddRange(
                debug.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        _settings.PollingIntervalMs = (int)PollingSlider.Value;

        _settings.Save();
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
}

// ── ViewModels ────────────────────────────────────────────────────────────────

public class CategoryGroupVm : INotifyPropertyChanged
{
    public string App      { get; init; } = "";
    public string Category { get; init; } = "";
    public List<ShortcutItemVm> Items { get; } = new();

    public string CountLabel => $"{Items.Count}件";

    private bool _isGroupChecked = true;
    public bool IsGroupChecked
    {
        get => _isGroupChecked;
        set { _isGroupChecked = value; OnPropertyChanged(); }
    }

    public void SetAllItems(bool visible)
    {
        foreach (var item in Items) item.SetVisibleSilent(visible);
        IsGroupChecked = visible;
    }

    public void RefreshGroupState()
    {
        var v = Items.Count == 0 || Items.All(i => i.IsVisible);
        _isGroupChecked = v;
        OnPropertyChanged(nameof(IsGroupChecked));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class ShortcutItemVm : INotifyPropertyChanged
{
    public string Id            { get; init; } = "";
    public string Label         { get; init; } = "";
    public string KeysDisplay   { get; init; } = "";
    public bool   IsUserDefined { get; init; }
    public CategoryGroupVm? ParentGroup { get; set; }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value) return;
            _isVisible = value;
            OnPropertyChanged();
        }
    }

    internal void SetVisibleSilent(bool v)
    {
        _isVisible = v;
        OnPropertyChanged(nameof(IsVisible));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

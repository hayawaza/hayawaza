using System.IO;
using System.Text.Json;
using ShortcutOverlay.Models;

namespace ShortcutOverlay.Services;

/// <summary>
/// shortcuts.json を読み込み、アプリ別・可視フィルタ適用後のビューモデルリストを返す。
/// </summary>
public class ShortcutDataService
{
    private List<ShortcutEntry> _entries = new();

    public ShortcutDataService()
    {
        Load();
    }

    private void Load()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "shortcuts.json");
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            _entries = JsonSerializer.Deserialize<List<ShortcutEntry>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();
        }
        catch
        {
            _entries = new();
        }
    }

    /// <summary>
    /// 指定アプリかつ非表示リストに含まれていないエントリを ViewModel に変換して返す。
    /// </summary>
    public List<ShortcutViewModel> GetVisible(string appProcessName, IEnumerable<string> hiddenIds)
    {
        var hidden = new HashSet<string>(hiddenIds, StringComparer.OrdinalIgnoreCase);
        return _entries
            .Where(e => e.App.Equals(appProcessName, StringComparison.OrdinalIgnoreCase)
                     && !hidden.Contains(e.Id))
            .Select(ShortcutViewModel.From)
            .ToList();
    }

    /// <summary>指定アプリの全エントリ（設定UI用）</summary>
    public List<ShortcutEntry> GetAll(string appProcessName) =>
        _entries
            .Where(e => e.App.Equals(appProcessName, StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>全エントリ（設定UI: ショートカット ON/OFF リスト用）</summary>
    public List<ShortcutEntry> GetAllEntries() => _entries.ToList();
}

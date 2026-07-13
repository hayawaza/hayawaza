using System.IO;
using System.Text.Json;
using ShortcutOverlay.Models;

namespace ShortcutOverlay.Services;

public class ShortcutDataService
{
    private static readonly string UserShortcutsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Hayawaza", "user_shortcuts.json");

    private List<ShortcutEntry> _builtIn = new();
    private List<ShortcutEntry> _user = new();

    public ShortcutDataService()
    {
        LoadBuiltIn();
        LoadUser();
    }

    private void LoadBuiltIn()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "shortcuts.json");
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            _builtIn = JsonSerializer.Deserialize<List<ShortcutEntry>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { _builtIn = new(); }
    }

    private void LoadUser()
    {
        try
        {
            if (!File.Exists(UserShortcutsPath)) return;
            var json = File.ReadAllText(UserShortcutsPath);
            _user = JsonSerializer.Deserialize<List<ShortcutEntry>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            foreach (var e in _user) e.IsUserDefined = true;
        }
        catch { _user = new(); }
    }

    private void SaveUser()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(UserShortcutsPath)!);
            var json = JsonSerializer.Serialize(_user, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(UserShortcutsPath, json);
        }
        catch { }
    }

    private List<ShortcutEntry> All => _builtIn.Concat(_user).ToList();

    public List<ShortcutViewModel> GetVisible(string appProcessName, IEnumerable<string> hiddenIds)
    {
        var hidden = new HashSet<string>(hiddenIds, StringComparer.OrdinalIgnoreCase);
        return All
            .Where(e => e.App.Equals(appProcessName, StringComparison.OrdinalIgnoreCase)
                     && !hidden.Contains(e.Id))
            .Select(ShortcutViewModel.From)
            .ToList();
    }

    public List<ShortcutEntry> GetAll(string appProcessName) =>
        All.Where(e => e.App.Equals(appProcessName, StringComparison.OrdinalIgnoreCase)).ToList();

    public List<ShortcutEntry> GetAllEntries() => All;

    public void AddUserEntry(ShortcutEntry entry)
    {
        entry.IsUserDefined = true;
        _user.Add(entry);
        SaveUser();
    }

    public void RemoveUserEntry(string id)
    {
        _user.RemoveAll(e => e.Id == id);
        SaveUser();
    }

    public void Reload()
    {
        LoadBuiltIn();
        LoadUser();
    }
}

using Velopack;
using Velopack.Sources;

namespace ShortcutOverlay;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.Startup += async (_, _) => await CheckForUpdatesAsync();
        app.Run();
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            var source = new GithubSource("https://github.com/hayawaza/hayawaza", null, false);
            var manager = new UpdateManager(source);
            var newVersion = await manager.CheckForUpdatesAsync();
            if (newVersion != null)
                await manager.DownloadUpdatesAsync(newVersion);
        }
        catch { }
    }
}

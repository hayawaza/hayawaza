using Velopack;

namespace ShortcutOverlay;

public class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.Run();
    }
}

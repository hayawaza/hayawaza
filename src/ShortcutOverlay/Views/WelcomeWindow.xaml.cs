using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ShortcutOverlay.Services;

namespace ShortcutOverlay.Views;

public partial class WelcomeWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int noBackdrop = 1; // DWMSBT_NONE
        DwmSetWindowAttribute(hwnd, 38, ref noBackdrop, sizeof(int));
    }

    public WelcomeWindow()
    {
        InitializeComponent();
        StartupCheck.IsChecked = StartupService.IsEnabled();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartupService.SetEnabled(StartupCheck.IsChecked == true);
        Close();
    }
}

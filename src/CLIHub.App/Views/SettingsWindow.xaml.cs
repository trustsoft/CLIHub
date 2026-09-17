using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using CLIHub.App.Interop;

namespace CLIHub.App.Views;

public partial class SettingsWindow : Window
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    public SettingsWindow()
    {
        InitializeComponent();
        VersionChipText.Text = "v" + App.ResolveAppVersion();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var preference = DwmwcpRound;
        _ = NativeMethods.DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e) => Close();
}

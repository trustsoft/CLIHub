namespace CLIHub.Windows;

using CLIHub.ViewModels;
using System.Windows;

/// <summary>
/// Interaction logic for LaunchWindow.xaml
/// </summary>
public partial class LaunchWindow : Window
{
    public LaunchWindow(LaunchWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide to tray instead of closing the application
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}

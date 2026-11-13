namespace CLIHub.Views;

using System.Windows;

using CLIHub.ViewModels;

/// <summary>
///   The Settings window: dark drawn chrome (no OS title bar) around sidebar-navigated pages. The
///   borderless window profile, rounded corners, minimum-size and maximize behavior, Escape
///   handling, and header drag come from the <see cref="ChromeWindow" /> base.
/// </summary>
public partial class SettingsWindow : ChromeWindow
{
    private readonly SettingsViewModel viewModel;

    /// <summary>
    ///   Creates the window and binds it to the settings view model.
    /// </summary>
    /// <param name="viewModel"> The settings view model. </param>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        this.InitializeComponent();

        this.viewModel = viewModel;
        this.DataContext = viewModel;
        viewModel.RequestClose += (_, _) => this.Close();
        this.Closed += (_, _) => this.viewModel.Dispose();
    }

    /// <summary>
    ///   Reloads the stored preferences and shows (or re-activates) the window. The window grows to
    ///   the launch window's height when that window is taller, but it never shrinks below its own
    ///   size.
    /// </summary>
    public void ShowSettings()
    {
        this.viewModel.Load();

        WindowChromeHelper.EnsureOwnedByLaunchWindow(this);

        if (Application.Current?.MainWindow is { } launch && launch.ActualHeight > this.Height)
        {
            this.Height = launch.ActualHeight;
        }

        WindowChromeHelper.ShowAndActivate(this);
    }

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => this.Close();
}

namespace CLIHub.Views;

using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using CLIHub.Interop;
using CLIHub.ViewModels;

/// <summary>
///   The Settings window: dark drawn chrome (no OS title bar) around sidebar-navigated pages.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    ///   Creates the window and binds it to the settings view model.
    /// </summary>
    /// <param name="viewModel"> The settings view model. </param>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) => Close();
    }

    /// <summary>
    ///   Reloads the stored preferences and shows (or re-activates) the window. The height matches
    ///   the launch window's current height, so the two windows read as the same surface.
    /// </summary>
    public void ShowSettings()
    {
        _viewModel.Load();

        // The launch window stays on top of other windows, so the Settings window has to be owned
        // by it to be visible above it.
        if (Owner is null && Application.Current?.MainWindow is { } owner && !ReferenceEquals(owner, this))
        {
            Owner = owner;
        }

        if (Application.Current?.MainWindow is { } launch)
        {
            Height = Math.Max(MinHeight, launch.ActualHeight);
        }

        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void OnSourceInitialized(object? sender, EventArgs e) =>
        DwmApi.TryRoundCorners(new WindowInteropHelper(this).EnsureHandle());

    /// <summary>
    ///   Moves the window while the drawn header is dragged. The close button handles the press
    ///   itself, so a click on it never starts a drag.
    /// </summary>
    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The button was released before the move started.
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Close();
        e.Handled = true;
    }
}

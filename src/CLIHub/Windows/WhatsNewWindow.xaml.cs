namespace CLIHub.Windows;

using CLIHub.Interop;
using CLIHub.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

/// <summary>
///   The What's New window: the user-facing release notes, newest version first.
/// </summary>
/// <remarks>
///   The window draws its own chrome — no OS title bar — following the launch window, but it is an
///   ordinary window otherwise: not always on top, and it stays open while the user works in it.
/// </remarks>
public partial class WhatsNewWindow : Window
{
    /// <summary>
    ///   Creates the window and binds it to the What's New view model.
    /// </summary>
    /// <param name="viewModel"> The What's New view model. </param>
    public WhatsNewWindow(WhatsNewViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }

    /// <summary>
    ///   Shows the window, or brings it to the front when it is already open.
    /// </summary>
    public void ShowNotes()
    {
        // The launch window stays on top of other windows, so this window has to be owned by it
        // to be visible above it.
        if (Owner is null && Application.Current?.MainWindow is { } owner && !ReferenceEquals(owner, this))
        {
            Owner = owner;
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

    private void OnInstallClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is WhatsNewViewModel viewModel)
        {
            viewModel.RequestInstallUpdate();
        }
    }

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

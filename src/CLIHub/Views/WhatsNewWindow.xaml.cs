namespace CLIHub.Views;

using System.Windows;

using CLIHub.ViewModels;

/// <summary>
///   The What's New window: the user-facing release notes, newest version first.
/// </summary>
/// <remarks>
///   The window draws its own chrome — no OS title bar — through the <see cref="ChromeWindow" />
///   base, but it is an ordinary window otherwise: not always on top, and it stays open while the
///   user works in it.
/// </remarks>
public partial class WhatsNewWindow : ChromeWindow
{
    /// <summary>
    ///   Creates the window and binds it to the What's New view model.
    /// </summary>
    /// <param name="viewModel"> The What's New view model. </param>
    public WhatsNewWindow(WhatsNewViewModel viewModel)
    {
        this.InitializeComponent();

        this.DataContext = viewModel;
    }

    /// <summary>
    ///   Shows the window, or brings it to the front when it is already open.
    /// </summary>
    public void ShowNotes()
    {
        WindowChromeHelper.EnsureOwnedByLaunchWindow(this);
        WindowChromeHelper.ShowAndActivate(this);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => this.Close();

    private void OnInstallClick(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is WhatsNewViewModel viewModel)
        {
            viewModel.RequestInstallUpdate();
        }
    }

    private void OnCheckClick(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is WhatsNewViewModel viewModel)
        {
            viewModel.RequestCheckForUpdates();
        }
    }
}

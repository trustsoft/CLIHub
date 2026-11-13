namespace CLIHub.Views;

using System.Windows;

using CLIHub.ViewModels;

/// <summary>
///   Browse Plugins dialog: displays the remote plugin feed with install/update actions. The
///   borderless window profile, rounded corners, minimum-size enforcement, Escape handling, and
///   header drag come from the <see cref="ChromeWindow" /> base.
/// </summary>
public partial class BrowsePluginsDialog : ChromeWindow
{
    /// <summary>
    ///   Creates the dialog.
    /// </summary>
    /// <param name="viewModel"> The dialog view model. </param>
    public BrowsePluginsDialog(BrowsePluginsDialogViewModel viewModel)
    {
        this.InitializeComponent();

        this.DataContext = viewModel;

        this.Loaded += async (sender, args) => await viewModel.LoadAsync();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

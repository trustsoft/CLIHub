namespace CLIHub.Views;

using System.Windows;

using CLIHub.ViewModels;

/// <summary>
///   Browse Plugins dialog: displays the remote plugin feed with install/update actions.
/// </summary>
public partial class BrowsePluginsDialog : Window
{
    private readonly BrowsePluginsDialogViewModel viewModel;

    /// <summary>
    ///   Creates the dialog.
    /// </summary>
    /// <param name="viewModel"> The dialog view model. </param>
    public BrowsePluginsDialog(BrowsePluginsDialogViewModel viewModel)
    {
        this.InitializeComponent();

        this.viewModel = viewModel;
        this.DataContext = viewModel;

        this.Loaded += async (sender, args) => await viewModel.LoadAsync();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}

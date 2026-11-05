namespace CLIHub.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

/// <summary>
///   Settings page for CLI agents: the default agent selector and the per-agent management cards.
/// </summary>
public partial class SettingsAgentsPage : UserControl
{
    /// <summary>
    ///   Creates the page.
    /// </summary>
    public SettingsAgentsPage() => this.InitializeComponent();

    /// <summary>
    ///   Right-aligns the plugin actions popover with the control that opened it. The popover
    ///   sizes to its content, so its width is measured here rather than fixed in XAML.
    /// </summary>
    private void OnActionsPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup || popup.Child is not FrameworkElement child)
        {
            return;
        }

        child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var targetWidth = popup.PlacementTarget is FrameworkElement target ? target.ActualWidth : 0;
        popup.HorizontalOffset = targetWidth - child.DesiredSize.Width;
    }

    /// <summary>
    ///   Closes the plugin actions menu on Escape while the keyboard focus is inside the popup. A
    ///   popup is a separate window, so its key events do not reach the Settings window's own
    ///   Escape handling.
    /// </summary>
    private void OnActionsMenuPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.PluginActionsPopup.IsOpen = false;
            e.Handled = true;
        }
    }

    /// <summary>
    ///   Closes the plugin actions menu after an entry is clicked. A popup does not close when an
    ///   entry inside it is clicked, so the click handler closes it.
    /// </summary>
    private void OnMenuActionInvoked(object sender, RoutedEventArgs e) => this.PluginActionsPopup.IsOpen = false;
}

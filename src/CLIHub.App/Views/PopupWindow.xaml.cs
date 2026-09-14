using System.Windows;
using System.Windows.Input;
using CLIHub.App.Interop;

namespace CLIHub.App.Views;

public partial class PopupWindow : Window
{
    public PopupWindow()
    {
        InitializeComponent();
    }

    public void ShowForHotkey()
    {
        ShowActivated = true;
        PopupPositioner.PositionCenteredOnCursorScreen(this);
        Show();
        Activate();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnDeactivated(object? sender, EventArgs e) => Hide();
}

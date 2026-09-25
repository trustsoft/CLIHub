using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using CLIHub.App.Interop;

namespace CLIHub.App.Views;

public partial class PopupWindow : Window
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    private bool _suppressHide;
    private bool _pinned;

    public PopupWindow()
    {
        InitializeComponent();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var preference = DwmwcpRound;
        _ = NativeMethods.DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
    }

    private void OnOpenProjectsMenu(object sender, RoutedEventArgs e)
    {
        if (ProjectsActionsButton.ContextMenu is { } menu)
        {
            menu.PlacementTarget = ProjectsActionsButton;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
            menu.HorizontalOffset = ProjectsActionsButton.ActualWidth - menu.ActualWidth;
        }
    }

    public void ShowForHotkey()
    {
        ShowActivated = true;
        PopupPositioner.PositionCenteredOnCursorScreen(this);
        Show();
        Activate();
    }

    public string? PickFolder()
    {
        _suppressHide = true;
        try
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Choose a project folder" };
            return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
        }
        finally
        {
            _suppressHide = false;
        }
    }

    public bool Confirm(string message)
    {
        _suppressHide = true;
        try
        {
            return MessageBox.Show(this, message, "CLIHub", MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes;
        }
        finally
        {
            _suppressHide = false;
        }
    }

    private void OnTogglePin(object sender, RoutedEventArgs e)
    {
        _pinned = PinToggleButton.IsChecked == true;
        PinGlyph.Text = _pinned ? "" : "";
        PinToggleButton.ToolTip = _pinned ? "Unpin" : "Pin";
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!_suppressHide && !_pinned)
        {
            Hide();
        }
    }
}

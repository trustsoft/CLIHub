using System.Windows;
using System.Windows.Input;
using CLIHub.App.Interop;

namespace CLIHub.App.Views;

public partial class PopupWindow : Window
{
    private bool _suppressHide;

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

    public string? PickFolder()
    {
        _suppressHide = true;
        try
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Выберите папку проекта" };
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
        if (!_suppressHide)
        {
            Hide();
        }
    }
}

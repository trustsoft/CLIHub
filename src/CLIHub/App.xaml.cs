using H.NotifyIcon;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CLIHub;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _taskbarIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        InitializeSystemTrayIcon();
    }

    private void InitializeSystemTrayIcon()
    {
        _taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
            ToolTipText = "CLIHub - AI Agent Launcher"
        };

        _taskbarIcon.TrayLeftMouseUp += (_, _) => ShowMainWindow();
        _taskbarIcon.ContextMenu = BuildContextMenu();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var launchItem = new MenuItem { Header = "Launch" };
        launchItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(launchItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null)
            return;

        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _taskbarIcon?.Dispose();
        base.OnExit(e);
    }
}

using H.NotifyIcon;
using Microsoft.Win32;
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

        AppServices.Projects.ProjectsChanged += (_, _) => RefreshTrayMenu();
        AppServices.Plugins.LoadPlugins();

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
        RefreshTrayMenu();
    }

    private void RefreshTrayMenu()
    {
        if (_taskbarIcon != null)
            _taskbarIcon.ContextMenu = BuildContextMenu();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var current = AppServices.Projects.GetCurrentProject();
        var currentItem = new MenuItem
        {
            Header = current != null ? $"Current: {current.Name}" : "No project selected",
            IsEnabled = false
        };
        menu.Items.Add(currentItem);

        var recentMenu = new MenuItem { Header = "Recent Projects" };
        var recent = AppServices.Projects.GetRecentProjects(10);
        if (recent.Count == 0)
        {
            recentMenu.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
        }
        else
        {
            foreach (var project in recent)
            {
                var item = new MenuItem
                {
                    Header = project.IsFavorite ? $"{project.Name} *" : project.Name
                };
                var id = project.Id;
                item.Click += (_, _) => AppServices.Projects.SetCurrentProject(id);
                recentMenu.Items.Add(item);
            }
        }
        menu.Items.Add(recentMenu);

        var addItem = new MenuItem { Header = "Add Project..." };
        addItem.Click += (_, _) => AddProject();
        menu.Items.Add(addItem);

        menu.Items.Add(new Separator());

        var launchItem = new MenuItem { Header = "Show CLIHub" };
        launchItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(launchItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void AddProject()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var project = AppServices.Projects.AddProject(dialog.FolderName);
                AppServices.Projects.SetCurrentProject(project.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not add project: {ex.Message}",
                    "CLIHub",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
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

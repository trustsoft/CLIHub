using CLIHub.Core.Interfaces;
using CLIHub.Windows;
using H.NotifyIcon;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CLIHub;

/// <summary>
/// Owns the system tray icon, its context menu, and main-window visibility.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly IProjectService _projects;
    private readonly MainWindow _mainWindow;
    private readonly TaskbarIcon _taskbarIcon;

    public TrayIconController(IProjectService projects, MainWindow mainWindow)
    {
        _projects = projects;
        _mainWindow = mainWindow;

        _taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
            ToolTipText = "CLIHub - AI Agent Launcher"
        };

        _taskbarIcon.TrayLeftMouseUp += (_, _) => ShowMainWindow();

        _projects.ProjectsChanged += (_, _) => RefreshMenu();

        RefreshMenu();
        _taskbarIcon.ForceCreate();
    }

    /// <summary>
    /// Shows and activates the main window.
    /// </summary>
    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void RefreshMenu()
    {
        _taskbarIcon.ContextMenu = BuildContextMenu();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var current = _projects.GetCurrentProject();
        menu.Items.Add(new MenuItem
        {
            Header = current != null ? $"Current: {current.Name}" : "No project selected",
            IsEnabled = false
        });

        var recentMenu = new MenuItem { Header = "Recent Projects" };
        var recent = _projects.GetRecentProjects(10);
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
                item.Click += (_, _) => _projects.SetCurrentProject(id);
                recentMenu.Items.Add(item);
            }
        }
        menu.Items.Add(recentMenu);

        var addItem = new MenuItem { Header = "Add Project..." };
        addItem.Click += (_, _) => AddProject();
        menu.Items.Add(addItem);

        menu.Items.Add(new Separator());

        var showItem = new MenuItem { Header = "Show CLIHub" };
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void AddProject()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var project = _projects.AddProject(dialog.FolderName);
            _projects.SetCurrentProject(project.Id);
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

    public void Dispose()
    {
        _taskbarIcon.Dispose();
    }
}

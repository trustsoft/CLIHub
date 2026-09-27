using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Win32;
using System.Windows;

namespace CLIHub.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IPluginManager _pluginManager;
    private readonly IProcessLauncher _processLauncher;
    private readonly IProjectService _projectService;

    public MainWindow()
    {
        InitializeComponent();

        _pluginManager = AppServices.Plugins;
        _processLauncher = AppServices.Launcher;
        _projectService = AppServices.Projects;

        _projectService.ProjectsChanged += (_, _) => RefreshProjects();

        RefreshProjects();
        LoadPlugins();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide to tray instead of closing the application
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }

    private void RefreshProjects()
    {
        var projects = _projectService.GetAllProjects().ToList();
        ProjectList.ItemsSource = projects;

        var current = _projectService.GetCurrentProject();
        if (current != null)
            ProjectList.SelectedItem = projects.FirstOrDefault(p => p.Id == current.Id);
    }

    private void LoadPlugins()
    {
        try
        {
            _pluginManager.LoadPlugins();
            var plugins = _pluginManager.GetAllPlugins().ToList();

            if (plugins.Count > 0)
            {
                StatusText.Text = $"Loaded {plugins.Count} plugin(s) - Ready to launch AI agents";
                PluginListText.Text = string.Join("\n", plugins.Select(p => $"- {p.Name}"));
            }
            else
            {
                StatusText.Text = "No plugins found. Check %APPDATA%\\CLIHub\\plugins\\";
                PluginListText.Text = "No plugins loaded.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading plugins: {ex.Message}";
            PluginListText.Text = "Error loading plugins.";
        }
    }

    private void ProjectList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProjectList.SelectedItem is Project project)
        {
            _projectService.SetCurrentProject(project.Id);
            StatusText.Text = $"Current project: {project.Name}";
        }
    }

    private void AddProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var project = _projectService.AddProject(dialog.FolderName);
                _projectService.SetCurrentProject(project.Id);
                StatusText.Text = $"Added project: {project.Name}";
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

    private void LaunchPlugin_Click(object sender, RoutedEventArgs e)
    {
        var currentProject = _projectService.GetCurrentProject();
        if (currentProject == null)
        {
            StatusText.Text = "Select a project before launching an agent.";
            MessageBox.Show(
                "Select a project before launching an agent.",
                "CLIHub",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var plugin = _pluginManager.GetAllPlugins().FirstOrDefault();
        var command = plugin?.Commands?.FirstOrDefault();

        if (plugin == null || command == null)
        {
            StatusText.Text = "No plugins available to launch.";
            return;
        }

        _projectService.TouchProject(currentProject.Id);

        var success = _processLauncher.LaunchProcess(command, currentProject.Path);

        StatusText.Text = success
            ? $"Launched {plugin.Name} in {currentProject.Name}"
            : $"Failed to launch {plugin.Name}";
    }
}

namespace CLIHub.Views;

using System.Windows;

using Microsoft.Win32;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using CLIHub.ViewModels;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IPluginManager _pluginManager;
    private readonly IProjectService _projectService;
    private readonly IAgentCommandService _agentCommandService;
    private readonly IAgentDetectionService _agentDetectionService;
    private readonly IAgentVersionService _agentVersionService;
    private readonly IConfigService _configService;
    private readonly IUpdateService _updateService;
    private bool _suppressFilterEvent;

    /// <summary>
    ///   Creates the window and performs the initial project and agent load.
    /// </summary>
    /// <param name="pluginManager"> Plugin manager for the agent list. </param>
    /// <param name="projectService"> Project service for the project list. </param>
    /// <param name="agentCommandService"> Service executing agent commands. </param>
    /// <param name="agentDetectionService"> Service detecting host and project availability. </param>
    /// <param name="agentVersionService"> Service resolving agent versions. </param>
    /// <param name="configService"> Configuration service for the filter preference. </param>
    /// <param name="updateService"> Update service for the version text and update checks. </param>
    public MainWindow(
        IPluginManager pluginManager,
        IProjectService projectService,
        IAgentCommandService agentCommandService,
        IAgentDetectionService agentDetectionService,
        IAgentVersionService agentVersionService,
        IConfigService configService,
        IUpdateService updateService)
    {
        InitializeComponent();

        _pluginManager = pluginManager;
        _projectService = projectService;
        _agentCommandService = agentCommandService;
        _agentDetectionService = agentDetectionService;
        _agentVersionService = agentVersionService;
        _configService = configService;
        _updateService = updateService;

        _projectService.ProjectsChanged += (_, _) => RefreshProjects();

        _suppressFilterEvent = true;
        FilterUnavailableCheckBox.IsChecked = _configService.Load().Preferences.ShowOnlyProjectAgents;
        _suppressFilterEvent = false;

        AppVersionText.Text = $"v{_updateService.GetCurrentVersion()}";

        RefreshProjects();
        RefreshAgents();
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Checking for updates...";

        var result = await _updateService.CheckForUpdatesAsync();

        StatusText.Text = result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        };
    }

    private void Filter_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressFilterEvent)
        {
            return;
        }

        var config = _configService.Load();
        config.Preferences.ShowOnlyProjectAgents = FilterUnavailableCheckBox.IsChecked == true;
        _configService.Save(config);

        RefreshAgents();
    }

    /// <summary>
    ///   Hides the window instead of closing it; the application lives in the tray.
    /// </summary>
    /// <param name="e"> Closing event arguments, always cancelled. </param>
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
        {
            ProjectList.SelectedItem = projects.FirstOrDefault(p => p.Id == current.Id);
        }
    }

    private void RefreshAgents()
    {
        var currentProject = _projectService.GetCurrentProject()?.Path;
        var filterUnavailable = FilterUnavailableCheckBox.IsChecked == true && currentProject != null;
        var items = new List<AgentItem>();

        foreach (var plugin in _pluginManager.GetAllPlugins())
        {
            var inSystem = _agentDetectionService.IsInstalledInSystem(plugin);
            var inProject = currentProject != null && _agentDetectionService.IsAvailableInProject(plugin, currentProject);

            if (filterUnavailable && !inProject)
            {
                continue;
            }

            var available = currentProject == null || inProject;

            items.Add(new AgentItem
            {
                Plugin = plugin,
                Name = plugin.Name,
                LogoPath = plugin.LogoPath,
                IsAvailable = available,
                Status = $"System: {(inSystem ? "yes" : "no")}  |  Project: {(inProject ? "yes" : "no")}"
            });
        }

        AgentList.ItemsSource = items;

        if (items.Count == 0)
        {
            StatusText.Text = "No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\";
        }
        else
        {
            _ = PopulateVersionsAsync(items);
        }
    }

    private async Task PopulateVersionsAsync(IReadOnlyList<AgentItem> items)
    {
        await Task.WhenAll(items.Select(async item =>
        {
            var version = await _agentVersionService.GetVersionAsync(item.Plugin);
            item.Version = version ?? "unknown";
        }));
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _agentVersionService.Invalidate();
        _agentDetectionService.Invalidate();
        RefreshAgents();
        StatusText.Text = "Refreshed agents, versions and availability.";
    }

    private void ProjectList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProjectList.SelectedItem is Project project)
        {
            _projectService.SetCurrentProject(project.Id);
            StatusText.Text = $"Current project: {project.Name}";
            RefreshAgents();
        }
    }

    private void AgentList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (AgentList.SelectedItem is AgentItem item)
        {
            StatusText.Text = $"Selected agent: {item.Name}";
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

    private void RemoveProject_Click(object sender, RoutedEventArgs e)
    {
        if (ProjectList.SelectedItem is not Project project)
        {
            StatusText.Text = "Select a project to remove.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Remove \"{project.Name}\" from CLIHub?\n\nThe folder and its files are not deleted.",
            "CLIHub",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        _projectService.RemoveProject(project.Id);
        StatusText.Text = $"Removed project: {project.Name}";
        RefreshAgents();
    }

    private void Launch_Click(object sender, RoutedEventArgs e) => ExecuteAsync(AgentCommandKind.Launch);
    private void Resume_Click(object sender, RoutedEventArgs e) => ExecuteAsync(AgentCommandKind.Resume);
    private void Init_Click(object sender, RoutedEventArgs e) => ExecuteAsync(AgentCommandKind.Init);
    private void Update_Click(object sender, RoutedEventArgs e) => ExecuteAsync(AgentCommandKind.Update);
    private void Version_Click(object sender, RoutedEventArgs e) => ExecuteAsync(AgentCommandKind.Version);

    private async void ExecuteAsync(AgentCommandKind kind)
    {
        if (AgentList.SelectedItem is not AgentItem item)
        {
            StatusText.Text = "Select an agent first.";
            return;
        }

        var project = _projectService.GetCurrentProject();
        if (project == null)
        {
            StatusText.Text = "Select a project before running an agent command.";
            MessageBox.Show(
                "Select a project before running an agent command.",
                "CLIHub",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        StatusText.Text = $"{kind} {item.Name}...";

        var result = await _agentCommandService.ExecuteAsync(item.Plugin, kind, project.Path);

        if (kind == AgentCommandKind.Version)
        {
            if (result.Success)
            {
                StatusText.Text = $"{item.Name} version: {result.Output}";
                MessageBox.Show(result.Output ?? string.Empty, $"{item.Name} version");
            }
            else
            {
                StatusText.Text = $"{item.Name} version failed: {result.Error}";
            }
        }
        else
        {
            StatusText.Text = result.Success
                ? $"{kind} started for {item.Name}"
                : $"{kind} failed: {result.Error}";
        }

        RefreshAgents();
    }
}

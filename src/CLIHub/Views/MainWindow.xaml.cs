namespace CLIHub.Views;

using System.Diagnostics;
using System.Windows;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using CLIHub;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;
using CLIHub.Core.Models;
using CLIHub.Lifetime;
using CLIHub.ViewModels;
using CLIHub.Updates;

/// <summary>
///   Interaction logic for MainWindow.xaml
/// </summary>
/// <remarks>
///   Deprecated legacy reference window. It is not registered or constructed by the application.
/// </remarks>
public partial class MainWindow : Window
{
    private readonly IPluginCatalog pluginCatalog;
    private readonly IProjectService projectService;
    private readonly IAgentCommandService agentCommandService;
    private readonly IAgentDetectionService agentDetectionService;
    private readonly IAgentVersionService agentVersionService;
    private readonly IPreferencesStore preferencesStore;
    private readonly IUpdateVersionProvider versionProvider;
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly ILogger<MainWindow> logger;
    private bool suppressFilterEvent;
    private CancellationTokenSource? versionPopulationCts;
    private int versionPopulationGeneration;

    /// <summary>
    ///   Creates the window and performs the initial project and agent load.
    /// </summary>
    /// <param name="pluginCatalog"> Plugin catalog for the agent list. </param>
    /// <param name="projectService"> Project service for the project list. </param>
    /// <param name="agentCommandService"> Service executing agent commands. </param>
    /// <param name="agentDetectionService"> Service detecting host and project availability. </param>
    /// <param name="agentVersionService"> Service resolving agent versions. </param>
    /// <param name="preferencesStore"> Store for the filter preference. </param>
    /// <param name="versionProvider"> Provides the version text. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="logger"> Logger for unexpected legacy window action failures. </param>
    public MainWindow(
        IPluginCatalog pluginCatalog,
        IProjectService projectService,
        IAgentCommandService agentCommandService,
        IAgentDetectionService agentDetectionService,
        IAgentVersionService agentVersionService,
        IPreferencesStore preferencesStore,
        IUpdateVersionProvider versionProvider,
        IUpdateWorkflow updateWorkflow,
        ILogger<MainWindow> logger)
    {
        this.InitializeComponent();

        this.pluginCatalog = pluginCatalog;
        this.projectService = projectService;
        this.agentCommandService = agentCommandService;
        this.agentDetectionService = agentDetectionService;
        this.agentVersionService = agentVersionService;
        this.preferencesStore = preferencesStore;
        this.versionProvider = versionProvider;
        this.updateWorkflow = updateWorkflow;
        this.logger = logger;

        this.projectService.ProjectsChanged += (_, _) => this.RefreshProjects();

        this.suppressFilterEvent = true;
        this.FilterUnavailableCheckBox.IsChecked = this.preferencesStore.Load().ShowOnlyProjectAgents;
        this.suppressFilterEvent = false;

        this.AppVersionText.Text = $"v{this.versionProvider.GetCurrentVersion()}";

        this.RefreshProjects();
        this.RefreshAgents();
    }

    private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        _ = AsyncOperationRunner.RunAsync(
            "Legacy window update check",
            this.CheckForUpdatesAsync,
            this.logger,
            message => this.StatusText.Text = message);
    }

    private async Task CheckForUpdatesAsync()
    {
        this.StatusText.Text = "Checking for updates...";

        var result = await this.updateWorkflow.CheckForUpdatesAsync();

        this.StatusText.Text = result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        };
    }

    private void Filter_Toggled(object sender, RoutedEventArgs e)
    {
        if (this.suppressFilterEvent)
        {
            return;
        }

        this.preferencesStore.Update(preferences =>
            preferences.ShowOnlyProjectAgents = this.FilterUnavailableCheckBox.IsChecked == true);

        this.RefreshAgents();
    }

    /// <summary>
    ///   Hides the window instead of closing it; the application lives in the tray.
    /// </summary>
    /// <param name="e"> Closing event arguments, always cancelled. </param>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Hide to tray instead of closing the application
        e.Cancel = true;
        this.Hide();
        base.OnClosing(e);
    }

    private void RefreshProjects()
    {
        var projects = this.projectService.GetAllProjects().ToList();
        this.ProjectList.ItemsSource = projects;

        var current = this.projectService.GetCurrentProject();
        if (current != null)
        {
            this.ProjectList.SelectedItem = projects.FirstOrDefault(p => p.Id == current.Id);
        }
    }

    private void RefreshAgents()
    {
        this.versionPopulationCts?.Cancel();
        this.versionPopulationCts?.Dispose();
        this.versionPopulationCts = new CancellationTokenSource();
        var generation = ++this.versionPopulationGeneration;
        var cancellationToken = this.versionPopulationCts.Token;

        var currentProject = this.projectService.GetCurrentProject()?.Path;
        var filterUnavailable = this.FilterUnavailableCheckBox.IsChecked == true && currentProject != null;
        var items = new List<AgentItem>();

        foreach (var plugin in this.pluginCatalog.GetAllPlugins())
        {
            var inSystem = this.agentDetectionService.IsInstalledInSystem(plugin);
            var inProject = currentProject != null && this.agentDetectionService.IsAvailableInProject(plugin, currentProject);

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

        this.AgentList.ItemsSource = items;

        if (items.Count == 0)
        {
            this.StatusText.Text = "No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\";
        }
        else
        {
            _ = this.PopulateVersionsAsync(items, generation, cancellationToken);
        }
    }

    private async Task PopulateVersionsAsync(
        IReadOnlyList<AgentItem> items,
        int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(items.Select(async item =>
            {
                var version = await this.agentVersionService.GetVersionAsync(item.Plugin, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && generation == this.versionPopulationGeneration)
                {
                    item.Version = version ?? "unknown";
                }
            }));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Agent version population failed: {ex}");
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        this.agentVersionService.Invalidate();
        this.agentDetectionService.Invalidate();
        this.RefreshAgents();
        this.StatusText.Text = "Refreshed agents, versions and availability.";
    }

    private void ProjectList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (this.ProjectList.SelectedItem is Project project)
        {
            this.projectService.SetCurrentProject(project.Id);
            this.StatusText.Text = $"Current project: {project.Name}";
            this.RefreshAgents();
        }
    }

    private void AgentList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (this.AgentList.SelectedItem is AgentItem item)
        {
            this.StatusText.Text = $"Selected agent: {item.Name}";
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
                var project = this.projectService.AddProject(dialog.FolderName);
                this.projectService.SetCurrentProject(project.Id);
                this.StatusText.Text = $"Added project: {project.Name}";
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
        if (this.ProjectList.SelectedItem is not Project project)
        {
            this.StatusText.Text = "Select a project to remove.";
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

        this.projectService.RemoveProject(project.Id);
        this.StatusText.Text = $"Removed project: {project.Name}";
        this.RefreshAgents();
    }

    private void Launch_Click(object sender, RoutedEventArgs e) => this.StartAgentCommand(AgentCommandKind.Launch);
    private void Resume_Click(object sender, RoutedEventArgs e) => this.StartAgentCommand(AgentCommandKind.Resume);
    private void Update_Click(object sender, RoutedEventArgs e) => this.StartAgentCommand(AgentCommandKind.Update);
    private void Version_Click(object sender, RoutedEventArgs e) => this.StartAgentCommand(AgentCommandKind.Version);

    private void StartAgentCommand(AgentCommandKind kind) =>
        _ = AsyncOperationRunner.RunAsync(
            $"Legacy window {kind} command",
            () => this.ExecuteAsync(kind),
            this.logger,
            message => this.StatusText.Text = message);

    private async Task ExecuteAsync(AgentCommandKind kind)
    {
        if (this.AgentList.SelectedItem is not AgentItem item)
        {
            this.StatusText.Text = "Select an agent first.";
            return;
        }

        var project = this.projectService.GetCurrentProject();
        if (project == null)
        {
            this.StatusText.Text = "Select a project before running an agent command.";
            MessageBox.Show(
                "Select a project before running an agent command.",
                "CLIHub",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        this.StatusText.Text = $"{kind} {item.Name}...";

        var result = await this.agentCommandService.ExecuteAsync(item.Plugin, kind, project.Path);

        if (kind == AgentCommandKind.Version)
        {
            if (result.Success)
            {
                this.StatusText.Text = $"{item.Name} version: {result.Output}";
                MessageBox.Show(result.Output ?? string.Empty, $"{item.Name} version");
            }
            else
            {
                this.StatusText.Text = $"{item.Name} version failed: {result.Error}";
            }
        }
        else
        {
            this.StatusText.Text = result.Success
                ? $"{kind} started for {item.Name}"
                : $"{kind} failed: {result.Error}";
        }

        this.RefreshAgents();
    }
}

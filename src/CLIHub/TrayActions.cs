namespace CLIHub;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;

/// <summary>
///   Coordinates application workflows exposed by the system tray.
/// </summary>
public sealed class TrayActions : ITrayActions, IDisposable
{
    private readonly IProjectService _projects;
    private readonly IPluginCatalog _pluginCatalog;
    private readonly IAgentCommandWorkflow _agentCommandWorkflow;
    private readonly IUpdateService _updates;
    private readonly IProjectDialogService _projectDialog;
    private readonly IUserNotificationService _notifications;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IReleaseNotesLauncher _releaseNotesLauncher;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public event EventHandler? UpdateDownloadRequested;

    /// <inheritdoc />
    public TrayMenuCommands Commands { get; }

    /// <summary>
    ///   Creates the tray application action coordinator.
    /// </summary>
    public TrayActions(
        IProjectService projects,
        IPluginCatalog pluginCatalog,
        IAgentCommandWorkflow agentCommandWorkflow,
        IUpdateService updates,
        IProjectDialogService projectDialog,
        IUserNotificationService notifications,
        ISettingsLauncher settingsLauncher,
        IReleaseNotesLauncher releaseNotesLauncher)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
        _agentCommandWorkflow = agentCommandWorkflow ?? throw new ArgumentNullException(nameof(agentCommandWorkflow));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        _projectDialog = projectDialog ?? throw new ArgumentNullException(nameof(projectDialog));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _settingsLauncher = settingsLauncher ?? throw new ArgumentNullException(nameof(settingsLauncher));
        _releaseNotesLauncher = releaseNotesLauncher ?? throw new ArgumentNullException(nameof(releaseNotesLauncher));

        _projects.ProjectsChanged += OnStateChanged;
        _pluginCatalog.PluginsChanged += OnStateChanged;
        _updates.UpdateStateChanged += OnUpdateStateChanged;

        Commands = new TrayMenuCommands(
            _projects.SetCurrentProject,
            LaunchAgentAsync,
            AddProject,
            _settingsLauncher.ShowSettings,
            _releaseNotesLauncher.ShowReleaseNotes,
            () => UpdateDownloadRequested?.Invoke(this, EventArgs.Empty),
            static () => { },
            static () => { });
    }

    /// <inheritdoc />
    public TrayMenuState GetState() => new(
        _projects.GetCurrentProject(),
        _projects.GetRecentProjects(10),
        _pluginCatalog.GetAllPlugins().Where(plugin => plugin.Commands?.Launch is not null).ToArray(),
        _updates.LastKnownAvailableVersion,
        _updates.IsDownloading);

    /// <summary>
    ///   Releases application event subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projects.ProjectsChanged -= OnStateChanged;
        _pluginCatalog.PluginsChanged -= OnStateChanged;
        _updates.UpdateStateChanged -= OnUpdateStateChanged;
    }

    private void AddProject()
    {
        var folder = _projectDialog.SelectProjectFolder();
        if (folder is null)
        {
            return;
        }

        try
        {
            var project = _projects.AddProject(folder);
            _projects.SetCurrentProject(project.Id);
        }
        catch (Exception ex)
        {
            _notifications.ShowWarning($"Could not add project: {ex.Message}");
        }
    }

    private async Task LaunchAgentAsync(Plugin plugin, Project project)
    {
        var result = await _agentCommandWorkflow.ExecuteAsync(plugin, project, AgentCommandKind.Launch);
        if (!result.Success)
        {
            _notifications.ShowWarning(result.Error ?? $"Failed to launch {plugin.Name}");
        }
    }

    private void OnStateChanged(object? sender, EventArgs e) => StateChanged?.Invoke(this, e);

    private void OnUpdateStateChanged(object? sender, EventArgs e) => StateChanged?.Invoke(this, e);
}

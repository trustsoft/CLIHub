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
    private readonly IUpdateStateSource _updates;
    private readonly IUpdateChecker _updateChecker;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IProjectDialogService _projectDialog;
    private readonly IUserNotificationService _notifications;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IReleaseNotesLauncher _releaseNotesLauncher;
    private bool _disposed;
    private bool _isCheckingForUpdates;

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
        IUpdateStateSource updates,
        IUpdateChecker updateChecker,
        IApplicationOperationLifetime operationLifetime,
        IProjectDialogService projectDialog,
        IUserNotificationService notifications,
        ISettingsLauncher settingsLauncher,
        IReleaseNotesLauncher releaseNotesLauncher)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
        _agentCommandWorkflow = agentCommandWorkflow ?? throw new ArgumentNullException(nameof(agentCommandWorkflow));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
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
            CheckForUpdates,
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
        _updates.IsDownloading,
        _isCheckingForUpdates);

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

    private void CheckForUpdates()
    {
        if (_isCheckingForUpdates || _updates.IsDownloading)
        {
            return;
        }

        _isCheckingForUpdates = true;
        StateChanged?.Invoke(this, EventArgs.Empty);
        _ = _operationLifetime.RunAsync("Manual update check", CheckForUpdatesAsync);
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _updateChecker.CheckForUpdatesAsync(cancellationToken);
        }
        finally
        {
            _isCheckingForUpdates = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnStateChanged(object? sender, EventArgs e) => StateChanged?.Invoke(this, e);

    private void OnUpdateStateChanged(object? sender, EventArgs e) => StateChanged?.Invoke(this, e);
}

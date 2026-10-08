namespace CLIHub;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;

/// <summary>
///   Executes user-invoked tray workflows without projecting tray state or owning WPF controls.
/// </summary>
public sealed class TrayCommandHandlers
{
    private readonly IProjectService _projects;
    private readonly IAgentCommandWorkflow _agentCommandWorkflow;
    private readonly IUpdateStateSource _updates;
    private readonly IUpdateChecker _updateChecker;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IProjectDialogService _projectDialog;
    private readonly IUserNotificationService _notifications;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IReleaseNotesLauncher _releaseNotesLauncher;
    private readonly IApplicationLifetime _applicationLifetime;
    private readonly TrayStateProjection _projection;

    /// <summary>
    ///   Raised when the user requests the existing update download workflow.
    /// </summary>
    public event EventHandler? UpdateDownloadRequested;

    /// <summary>
    ///   Creates the tray command handlers over application workflows and user-facing ports.
    /// </summary>
    /// <param name="projects"> Project operations. </param>
    /// <param name="agentCommandWorkflow"> Shared agent command workflow. </param>
    /// <param name="updates"> Shared update state. </param>
    /// <param name="updateChecker"> Manual update check workflow. </param>
    /// <param name="operationLifetime"> Lifetime tracking asynchronous tray operations. </param>
    /// <param name="projectDialog"> Project folder selection dialog. </param>
    /// <param name="notifications"> User-facing warning notifications. </param>
    /// <param name="settingsLauncher"> Settings launcher. </param>
    /// <param name="releaseNotesLauncher"> What's New launcher. </param>
    /// <param name="applicationLifetime"> Application shutdown port. </param>
    /// <param name="projection"> Shared tray state projection. </param>
    public TrayCommandHandlers(
        IProjectService projects,
        IAgentCommandWorkflow agentCommandWorkflow,
        IUpdateStateSource updates,
        IUpdateChecker updateChecker,
        IApplicationOperationLifetime operationLifetime,
        IProjectDialogService projectDialog,
        IUserNotificationService notifications,
        ISettingsLauncher settingsLauncher,
        IReleaseNotesLauncher releaseNotesLauncher,
        IApplicationLifetime applicationLifetime,
        TrayStateProjection projection)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _agentCommandWorkflow = agentCommandWorkflow ?? throw new ArgumentNullException(nameof(agentCommandWorkflow));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _projectDialog = projectDialog ?? throw new ArgumentNullException(nameof(projectDialog));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _settingsLauncher = settingsLauncher ?? throw new ArgumentNullException(nameof(settingsLauncher));
        _releaseNotesLauncher = releaseNotesLauncher ?? throw new ArgumentNullException(nameof(releaseNotesLauncher));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _projection = projection ?? throw new ArgumentNullException(nameof(projection));
    }

    /// <summary>
    ///   Creates the action set projected into the tray menu.
    /// </summary>
    /// <param name="selectProject"> Selects a registered project. </param>
    /// <param name="showLaunchWindow"> Shows the launch window. </param>
    /// <returns> Tray menu action delegates. </returns>
    public TrayMenuCommands CreateCommands(Action<string> selectProject, Action showLaunchWindow) => new(
        selectProject,
        LaunchAgentAsync,
        AddProject,
        _settingsLauncher.ShowSettings,
        _releaseNotesLauncher.ShowReleaseNotes,
        CheckForUpdates,
        () => UpdateDownloadRequested?.Invoke(this, EventArgs.Empty),
        showLaunchWindow,
        _applicationLifetime.Shutdown);

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
        if (_projection.IsCheckingForUpdates || _updates.IsDownloading)
        {
            return;
        }

        _projection.SetCheckingForUpdates(true);
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
            _projection.SetCheckingForUpdates(false);
        }
    }
}

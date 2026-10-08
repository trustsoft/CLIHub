namespace CLIHub;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;

/// <summary>
///   Executes user-invoked tray workflows without projecting tray state or owning WPF controls.
/// </summary>
public sealed class TrayCommandHandlers
{
    private readonly IProjectService _projects;
    private readonly IAgentCommandWorkflow _agentCommandWorkflow;
    private readonly IUpdateWorkflow _updateWorkflow;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IProjectDialogService _projectDialog;
    private readonly IUserNotificationService _notifications;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IReleaseNotesLauncher _releaseNotesLauncher;
    private readonly IApplicationLifetime _applicationLifetime;

    /// <summary>
    ///   Raised when the user requests the existing update download workflow.
    /// </summary>
    public event EventHandler? UpdateDownloadRequested;

    /// <summary>
    ///   Creates the tray command handlers over application workflows and user-facing ports.
    /// </summary>
    /// <param name="projects"> Project operations. </param>
    /// <param name="agentCommandWorkflow"> Shared agent command workflow. </param>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="operationLifetime"> Lifetime tracking asynchronous tray operations. </param>
    /// <param name="projectDialog"> Project folder selection dialog. </param>
    /// <param name="notifications"> User-facing warning notifications. </param>
    /// <param name="settingsLauncher"> Settings launcher. </param>
    /// <param name="releaseNotesLauncher"> What's New launcher. </param>
    /// <param name="applicationLifetime"> Application shutdown port. </param>
    public TrayCommandHandlers(
        IProjectService projects,
        IAgentCommandWorkflow agentCommandWorkflow,
        IUpdateWorkflow updateWorkflow,
        IApplicationOperationLifetime operationLifetime,
        IProjectDialogService projectDialog,
        IUserNotificationService notifications,
        ISettingsLauncher settingsLauncher,
        IReleaseNotesLauncher releaseNotesLauncher,
        IApplicationLifetime applicationLifetime)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _agentCommandWorkflow = agentCommandWorkflow ?? throw new ArgumentNullException(nameof(agentCommandWorkflow));
        _updateWorkflow = updateWorkflow ?? throw new ArgumentNullException(nameof(updateWorkflow));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _projectDialog = projectDialog ?? throw new ArgumentNullException(nameof(projectDialog));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _settingsLauncher = settingsLauncher ?? throw new ArgumentNullException(nameof(settingsLauncher));
        _releaseNotesLauncher = releaseNotesLauncher ?? throw new ArgumentNullException(nameof(releaseNotesLauncher));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
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
        if (_updateWorkflow.IsCheckingForUpdates || _updateWorkflow.IsDownloading)
        {
            return;
        }

        _ = _operationLifetime.RunAsync("Manual update check", CheckForUpdatesAsync);
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _updateWorkflow.CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}

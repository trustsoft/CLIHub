namespace CLIHub.Tray;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Lifetime;
using CLIHub.Updates;
using CLIHub.Settings;
using CLIHub.Launching;

/// <summary>
///   Executes user-invoked tray workflows without projecting tray state or owning WPF controls.
/// </summary>
public sealed class TrayCommandHandlers
{
    private readonly IProjectService projects;
    private readonly IAgentCommandWorkflow agentCommandWorkflow;
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly IProjectDialogService projectDialog;
    private readonly IUserNotificationService notifications;
    private readonly ISettingsLauncher settingsLauncher;
    private readonly IReleaseNotesLauncher releaseNotesLauncher;
    private readonly IApplicationLifetime applicationLifetime;

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
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.agentCommandWorkflow = agentCommandWorkflow ?? throw new ArgumentNullException(nameof(agentCommandWorkflow));
        this.updateWorkflow = updateWorkflow ?? throw new ArgumentNullException(nameof(updateWorkflow));
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        this.projectDialog = projectDialog ?? throw new ArgumentNullException(nameof(projectDialog));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        this.settingsLauncher = settingsLauncher ?? throw new ArgumentNullException(nameof(settingsLauncher));
        this.releaseNotesLauncher = releaseNotesLauncher ?? throw new ArgumentNullException(nameof(releaseNotesLauncher));
        this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
    }

    /// <summary>
    ///   Creates the action set projected into the tray menu.
    /// </summary>
    /// <param name="selectProject"> Selects a registered project. </param>
    /// <param name="showLaunchWindow"> Shows the launch window. </param>
    /// <returns> Tray menu action delegates. </returns>
    public TrayMenuCommands CreateCommands(Action<string> selectProject, Action showLaunchWindow) => new(
        selectProject,
        this.LaunchAgentAsync,
        this.AddProject,
        this.settingsLauncher.ShowSettings,
        this.releaseNotesLauncher.ShowReleaseNotes,
        this.CheckForUpdates,
        () => this.UpdateDownloadRequested?.Invoke(this, EventArgs.Empty),
        showLaunchWindow,
        this.applicationLifetime.Shutdown);

    private void AddProject()
    {
        var folder = this.projectDialog.SelectProjectFolder();
        if (folder is null)
        {
            return;
        }

        try
        {
            var project = this.projects.AddProject(folder);
            this.projects.SetCurrentProject(project.Id);
        }
        catch (Exception ex)
        {
            this.notifications.ShowWarning($"Could not add project: {ex.Message}");
        }
    }

    private async Task LaunchAgentAsync(Plugin plugin, Project project)
    {
        var result = await this.agentCommandWorkflow.ExecuteAsync(plugin, project, AgentCommandKind.Launch);
        if (!result.Success)
        {
            this.notifications.ShowWarning(result.Error ?? $"Failed to launch {plugin.Name}");
        }
    }

    private void CheckForUpdates()
    {
        if (this.updateWorkflow.IsCheckingForUpdates || this.updateWorkflow.IsDownloading)
        {
            return;
        }

        _ = this.operationLifetime.RunAsync("Manual update check", this.CheckForUpdatesAsync);
    }

    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await this.updateWorkflow.CheckForUpdatesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}

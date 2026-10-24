namespace CLIHub.ViewModels;

using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;

/// <summary>
///   Coordinates window-level actions and agent commands for the launch window.
///   Owns command instances and delegates execution to specialized coordinators.
/// </summary>
public sealed class WindowActionCoordinator
{
    private readonly LaunchCommandCoordinator launchCoordinator;
    private readonly StatusMessageCoordinator statusCoordinator;
    private readonly AgentProcessMonitor processMonitor;
    private readonly ISettingsLauncher settingsLauncher;
    private readonly IApplicationLifetime applicationLifetime;
    private readonly IExternalLauncher externalLauncher;
    private readonly Func<AgentItem?> getSelectedAgent;
    private readonly Func<Project?> getCurrentProject;
    private readonly Action refreshAgents;

    /// <summary>
    ///   Creates the window action coordinator with its dependencies and state providers.
    /// </summary>
    /// <param name="launchCoordinator"> Agent command execution coordinator. </param>
    /// <param name="statusCoordinator"> Status message coordinator for outcome reporting. </param>
    /// <param name="processMonitor"> Agent process monitor for update safety checks. </param>
    /// <param name="settingsLauncher"> Settings window launcher. </param>
    /// <param name="applicationLifetime"> Application lifetime control for exit. </param>
    /// <param name="externalLauncher"> Operating-system path launcher. </param>
    /// <param name="getSelectedAgent"> Function providing the currently selected agent. </param>
    /// <param name="getCurrentProject"> Function providing the current project. </param>
    /// <param name="refreshAgents"> Callback to refresh the agent pane after command execution. </param>
    public WindowActionCoordinator(LaunchCommandCoordinator launchCoordinator,
                                   StatusMessageCoordinator statusCoordinator,
                                   AgentProcessMonitor processMonitor,
                                   ISettingsLauncher settingsLauncher,
                                   IApplicationLifetime applicationLifetime,
                                   IExternalLauncher externalLauncher,
                                   Func<AgentItem?> getSelectedAgent,
                                   Func<Project?> getCurrentProject,
                                   Action refreshAgents)
    {
        this.launchCoordinator = launchCoordinator ?? throw new ArgumentNullException(nameof(launchCoordinator));
        this.statusCoordinator = statusCoordinator ?? throw new ArgumentNullException(nameof(statusCoordinator));
        this.processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
        this.settingsLauncher = settingsLauncher ?? throw new ArgumentNullException(nameof(settingsLauncher));
        this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        this.externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
        this.getSelectedAgent = getSelectedAgent ?? throw new ArgumentNullException(nameof(getSelectedAgent));
        this.getCurrentProject = getCurrentProject ?? throw new ArgumentNullException(nameof(getCurrentProject));
        this.refreshAgents = refreshAgents ?? throw new ArgumentNullException(nameof(refreshAgents));

        this.OpenDataFolderCommand = new RelayCommand(this.OpenDataFolder);
        this.OpenSettingsCommand = new RelayCommand(() => this.settingsLauncher.ShowSettings());
        this.ExitCommand = new RelayCommand(this.applicationLifetime.Shutdown);

        this.LaunchCommand = new RelayCommand(
            () => _ = this.RunAgentCommandAsync(this.getSelectedAgent(), AgentCommandKind.Launch),
            this.HasSelectedAgent);
        this.ResumeCommand = new RelayCommand(
            () => _ = this.RunAgentCommandAsync(this.getSelectedAgent(), AgentCommandKind.Resume),
            () => this.getSelectedAgent()?.CanResume ?? false);
        this.UpdateCommand = new RelayCommand(
            this.ExecuteUpdate,
            this.CanExecuteUpdate);
        this.VersionCommand = new RelayCommand(
            () => _ = this.RunAgentCommandAsync(this.getSelectedAgent(), AgentCommandKind.Version),
            () => this.getSelectedAgent()?.CanShowVersion ?? false);

        this.LaunchAgentCommand = new RelayCommand<AgentItem>(
            item => _ = this.RunAgentCommandAsync(item, AgentCommandKind.Launch),
            item => item.CanLaunch);
        this.ResumeAgentCommand = new RelayCommand<AgentItem>(
            item => _ = this.RunAgentCommandAsync(item, AgentCommandKind.Resume),
            item => item.CanResume);
    }

    /// <summary>
    ///   Opens the CLIHub data folder in Explorer.
    /// </summary>
    public RelayCommand OpenDataFolderCommand { get; }

    /// <summary>
    ///   Opens the Settings window.
    /// </summary>
    public RelayCommand OpenSettingsCommand { get; }

    /// <summary>
    ///   Exits the application.
    /// </summary>
    public RelayCommand ExitCommand { get; }

    /// <summary>
    ///   Launches the selected agent in the current project.
    /// </summary>
    public RelayCommand LaunchCommand { get; }

    /// <summary>
    ///   Resumes the selected agent in the current project.
    /// </summary>
    public RelayCommand ResumeCommand { get; }

    /// <summary>
    ///   Updates the selected agent.
    /// </summary>
    public RelayCommand UpdateCommand { get; }

    /// <summary>
    ///   Reports the selected agent's version in the status line.
    /// </summary>
    public RelayCommand VersionCommand { get; }

    /// <summary>
    ///   Launches the agent of the activated row.
    /// </summary>
    public RelayCommand<AgentItem> LaunchAgentCommand { get; }

    /// <summary>
    ///   Resumes the agent of the activated row.
    /// </summary>
    public RelayCommand<AgentItem> ResumeAgentCommand { get; }

    private void OpenDataFolder()
    {
        var root = AppPaths.Root;

        try
        {
            this.externalLauncher.Open(root);
            this.statusCoordinator.ReportFolderOpen(root);
        }
        catch (Exception ex)
        {
            this.statusCoordinator.ReportFolderOpen(root, ex);
        }
    }

    private bool HasSelectedAgent() => this.getSelectedAgent() is not null;

    private bool CanExecuteUpdate()
    {
        var agent = this.getSelectedAgent();
        if (agent is null || !agent.CanUpdate)
        {
            return false;
        }

        // Advisory early-out only: this requery check runs on every WPF CommandManager requery,
        // so it uses the cheap executable-name tier (no command-line retrieval). A runtime-hosted
        // agent (for example a node.exe behind an npm shim) can leave the button enabled; the
        // execution-time gate in ExecuteUpdate blocks that update authoritatively (design D3).
        if (this.processMonitor.HasRunningAgents(agent.Plugin, AgentProcessMatchDetail.ExecutableName))
        {
            return false;
        }

        return true;
    }

    private void ExecuteUpdate()
    {
        var agent = this.getSelectedAgent();
        if (agent is null || !agent.CanUpdate)
        {
            return;
        }

        // Cheap name-only fast block: a directly matched agent process reports the block without
        // paying the full command-line pass on the UI thread.
        if (this.processMonitor.HasRunningAgents(agent.Plugin, AgentProcessMatchDetail.ExecutableName))
        {
            this.statusCoordinator.ReportUpdateBlockedByRunningAgents([agent.Plugin.Name]);
            return;
        }

        _ = this.ExecuteUpdateAsync(agent);
    }

    private async Task ExecuteUpdateAsync(AgentItem agent)
    {
        try
        {
            // Authoritative full-fidelity gate (design D3/D7): a runtime-hosted agent whose
            // command line names the agent's npm package is caught here even though the advisory
            // requery check saw nothing. Task.Run keeps the command-line retrieval off the UI
            // thread.
            var runningAgents = await Task.Run(() => this.processMonitor.GetRunningAgents(agent.Plugin));
            if (runningAgents.Count > 0)
            {
                var agentNames = runningAgents.Select(runningAgent => runningAgent.Plugin.Name).ToList();
                this.statusCoordinator.ReportUpdateBlockedByRunningAgents(agentNames);
                return;
            }

            _ = this.RunAgentCommandAsync(agent, AgentCommandKind.Update);
        }
        catch (Exception ex)
        {
            // Nothing may escape a fire-and-forget continuation; the coordinator's existing
            // status surface is the failure report (no new logger dependency).
            this.statusCoordinator.ReportCommandOutcome(
                $"Could not check for running agents before the update: {ex.Message}");
        }
    }

    private Task RunAgentCommandAsync(AgentItem? item, AgentCommandKind kind) =>
        this.launchCoordinator.RunAsync(
            item,
            this.getCurrentProject(),
            kind,
            this.statusCoordinator.ReportCommandOutcome,
            this.refreshAgents);
}

namespace CLIHub.ViewModels;

using CLIHub;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;

/// <summary>
///   Coordinates window-level actions and agent commands for the launch window.
///   Owns command instances and delegates execution to specialized coordinators.
/// </summary>
public sealed class WindowActionCoordinator
{
    private readonly LaunchCommandCoordinator _launchCoordinator;
    private readonly StatusMessageCoordinator _statusCoordinator;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IApplicationLifetime _applicationLifetime;
    private readonly IExternalLauncher _externalLauncher;
    private readonly Func<AgentItem?> _getSelectedAgent;
    private readonly Func<Project?> _getCurrentProject;
    private readonly Action _refreshAgents;

    /// <summary>
    ///   Creates the window action coordinator with its dependencies and state providers.
    /// </summary>
    /// <param name="launchCoordinator"> Agent command execution coordinator. </param>
    /// <param name="statusCoordinator"> Status message coordinator for outcome reporting. </param>
    /// <param name="settingsLauncher"> Settings window launcher. </param>
    /// <param name="applicationLifetime"> Application lifetime control for exit. </param>
    /// <param name="externalLauncher"> Operating-system path launcher. </param>
    /// <param name="getSelectedAgent"> Function providing the currently selected agent. </param>
    /// <param name="getCurrentProject"> Function providing the current project. </param>
    /// <param name="refreshAgents"> Callback to refresh the agent pane after command execution. </param>
    public WindowActionCoordinator(
        LaunchCommandCoordinator launchCoordinator,
        StatusMessageCoordinator statusCoordinator,
        ISettingsLauncher settingsLauncher,
        IApplicationLifetime applicationLifetime,
        IExternalLauncher externalLauncher,
        Func<AgentItem?> getSelectedAgent,
        Func<Project?> getCurrentProject,
        Action refreshAgents)
    {
        _launchCoordinator = launchCoordinator ?? throw new ArgumentNullException(nameof(launchCoordinator));
        _statusCoordinator = statusCoordinator ?? throw new ArgumentNullException(nameof(statusCoordinator));
        _settingsLauncher = settingsLauncher ?? throw new ArgumentNullException(nameof(settingsLauncher));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));
        _getSelectedAgent = getSelectedAgent ?? throw new ArgumentNullException(nameof(getSelectedAgent));
        _getCurrentProject = getCurrentProject ?? throw new ArgumentNullException(nameof(getCurrentProject));
        _refreshAgents = refreshAgents ?? throw new ArgumentNullException(nameof(refreshAgents));

        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenSettingsCommand = new RelayCommand(() => _settingsLauncher.ShowSettings());
        ExitCommand = new RelayCommand(_applicationLifetime.Shutdown);

        LaunchCommand = new RelayCommand(
            () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Launch),
            HasSelectedAgent);
        ResumeCommand = new RelayCommand(
            () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Resume),
            HasSelectedAgent);
        InitCommand = new RelayCommand(
            () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Init),
            HasSelectedAgent);
        UpdateCommand = new RelayCommand(
            () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Update),
            HasSelectedAgent);
        VersionCommand = new RelayCommand(
            () => _ = RunAgentCommandAsync(_getSelectedAgent(), AgentCommandKind.Version),
            HasSelectedAgent);

        LaunchAgentCommand = new RelayCommand<AgentItem>(
            item => _ = RunAgentCommandAsync(item, AgentCommandKind.Launch),
            item => item.CanLaunch);
        ResumeAgentCommand = new RelayCommand<AgentItem>(
            item => _ = RunAgentCommandAsync(item, AgentCommandKind.Resume),
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
    ///   Initializes the selected agent in the current project.
    /// </summary>
    public RelayCommand InitCommand { get; }

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
            _externalLauncher.Open(root);
            _statusCoordinator.ReportFolderOpen(root);
        }
        catch (Exception ex)
        {
            _statusCoordinator.ReportFolderOpen(root, ex);
        }
    }

    private bool HasSelectedAgent() => _getSelectedAgent() is not null;

    private Task RunAgentCommandAsync(AgentItem? item, AgentCommandKind kind) =>
        _launchCoordinator.RunAsync(
            item,
            _getCurrentProject(),
            kind,
            _statusCoordinator.ReportCommandOutcome,
            _refreshAgents);
}

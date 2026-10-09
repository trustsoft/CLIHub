namespace CLIHub.ViewModels;

using CLIHub;

/// <summary>
///   Coordinates status messages from multiple sources: controller events,
///   command outcomes, preference changes, and error conditions.
/// </summary>
public sealed class StatusMessageCoordinator : ObservableObject, IDisposable
{
    private readonly ProjectPaneController _projectPane;
    private readonly AgentPaneController _agentPane;
    private readonly UpdateControlViewModel _updateControl;
    private string _currentMessage = "CLIHub ready";
    private bool _disposed;

    /// <summary>
    ///   Creates a status message coordinator that subscribes to events from controllers
    ///   and the update control.
    /// </summary>
    /// <param name="projectPane"> Project pane controller for project change events. </param>
    /// <param name="agentPane"> Agent pane controller (currently unused, reserved for future events). </param>
    /// <param name="updateControl"> Update control for outcome reporting. </param>
    public StatusMessageCoordinator(
        ProjectPaneController projectPane,
        AgentPaneController agentPane,
        UpdateControlViewModel updateControl)
    {
        _projectPane = projectPane ?? throw new ArgumentNullException(nameof(projectPane));
        _agentPane = agentPane ?? throw new ArgumentNullException(nameof(agentPane));
        _updateControl = updateControl ?? throw new ArgumentNullException(nameof(updateControl));

        _projectPane.ProjectsChanged += OnProjectsChanged;
        _updateControl.OutcomeReported += OnUpdateOutcomeReported;
    }

    /// <summary>
    ///   Current status message displayed in the footer.
    /// </summary>
    public string CurrentMessage
    {
        get => _currentMessage;
        private set => SetProperty(ref _currentMessage, value);
    }

    /// <summary>
    ///   Reports a project selection status.
    /// </summary>
    /// <param name="projectName"> Name of the selected project. </param>
    public void ReportProjectSelected(string projectName) =>
        CurrentMessage = $"Current project: {projectName}";

    /// <summary>
    ///   Reports an agent selection status.
    /// </summary>
    /// <param name="agentName"> Name of the selected agent. </param>
    public void ReportAgentSelected(string agentName) =>
        CurrentMessage = $"Selected agent: {agentName}";

    /// <summary>
    ///   Reports a window pin status change.
    /// </summary>
    /// <param name="isPinned"> Whether the window is now pinned. </param>
    public void ReportWindowPinChanged(bool isPinned) =>
        CurrentMessage = isPinned ? "Window pinned open" : "Window unpinned";

    /// <summary>
    ///   Reports a command execution outcome.
    /// </summary>
    /// <param name="message"> The outcome message to display. </param>
    public void ReportCommandOutcome(string message) =>
        CurrentMessage = message;

    /// <summary>
    ///   Reports a folder open success or error.
    /// </summary>
    /// <param name="path"> The folder path that was opened or attempted. </param>
    /// <param name="error"> The exception if the operation failed, or null if successful. </param>
    public void ReportFolderOpen(string path, Exception? error = null) =>
        CurrentMessage = error is null
            ? $"Opened {path}"
            : $"Could not open {path}: {error.Message}";

    /// <summary>
    ///   Reports when no agents are found.
    /// </summary>
    public void ReportNoAgentsFound() =>
        CurrentMessage = "No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\";

    private void OnProjectsChanged(object? sender, EventArgs e) =>
        CurrentMessage = "Projects refreshed";

    private void OnUpdateOutcomeReported(object? sender, string message) =>
        CurrentMessage = message;

    /// <summary>
    ///   Unsubscribes from all event sources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projectPane.ProjectsChanged -= OnProjectsChanged;
        _updateControl.OutcomeReported -= OnUpdateOutcomeReported;
    }
}

namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;

using CLIHub;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;
using CLIHub.Core.Formatting;

/// <summary>
///   State and commands for the launch window: the project and agent lists, the current
///   selection, the availability filter, the path display style, the pin state, the status
///   message, and every window action.
/// </summary>
public sealed class LaunchWindowViewModel : ObservableObject, IPathDisplayStyleTarget, IDisposable
{
    private const string NoProjectSelectedMessage = "Select a project first.";

    private readonly ProjectPaneController _projectPane;
    private readonly AgentPaneController _agentPane;
    private readonly IPreferencesStore _preferencesStore;
    private readonly LaunchWindowActionBuilder _actionBuilder;
    private readonly IUserNotificationService _notifications;
    private readonly MenuActionCoordinator _menuCoordinator;
    private readonly StatusMessageCoordinator _statusCoordinator;
    private readonly WindowActionCoordinator _windowActions;

    private Project? _selectedProject;
    private AgentItem? _selectedAgent;
    private bool _showOnlyProjectAgents;
    private bool _isPinned;
    private PathDisplayStyle _displayStyle = PathDisplayStyles.Default;
    private bool _suppressSelectionChange;
    private bool _suppressFilterChange;
    private bool _disposed;

    /// <summary>
    ///   Creates the view model with its services and loads projects and agents.
    /// </summary>
    /// <param name="projectPane"> Project-pane workflow boundary. </param>
    /// <param name="agentPane"> Agent-pane workflow boundary. </param>
    /// <param name="launchCommandCoordinator"> Agent-command workflow and result coordinator. </param>
    /// <param name="preferencesStore"> Store for persisted preferences. </param>
    /// <param name="actionBuilder"> Action builder for menu construction. </param>
    /// <param name="updateControl"> Shared update control for this launch window. </param>
    /// <param name="settingsLauncher"> Settings window launcher. </param>
    /// <param name="notifications"> Information and warning notifications. </param>
    /// <param name="applicationLifetime"> Application lifetime control. </param>
    /// <param name="externalLauncher"> Operating-system path launcher. </param>
    /// <param name="statusCoordinator"> Status message coordinator for the footer. </param>
    public LaunchWindowViewModel(
        ProjectPaneController projectPane,
        AgentPaneController agentPane,
        LaunchCommandCoordinator launchCommandCoordinator,
        IPreferencesStore preferencesStore,
        LaunchWindowActionBuilder actionBuilder,
        UpdateControlViewModel updateControl,
        ISettingsLauncher settingsLauncher,
        IUserNotificationService notifications,
        IApplicationLifetime applicationLifetime,
        IExternalLauncher externalLauncher,
        StatusMessageCoordinator statusCoordinator)
    {
        _projectPane = projectPane;
        _agentPane = agentPane;
        _preferencesStore = preferencesStore;
        _actionBuilder = actionBuilder;
        _notifications = notifications;
        _statusCoordinator = statusCoordinator;

        _statusCoordinator.PropertyChanged += OnStatusCoordinatorPropertyChanged;

        UpdateControl = updateControl;

        _windowActions = new WindowActionCoordinator(
            launchCommandCoordinator,
            statusCoordinator,
            settingsLauncher,
            applicationLifetime,
            externalLauncher,
            () => SelectedAgent,
            () => _projectPane.CurrentProject,
            RefreshAgents);

        AddProjectCommand = _projectPane.AddCommand;
        RemoveProjectCommand = _projectPane.RemoveCommand;
        ToggleFavoriteCommand = _projectPane.ToggleFavoriteCommand;
        RefreshCommand = _projectPane.RefreshCommand;

        var preferences = _preferencesStore.Load();

        _menuCoordinator = new MenuActionCoordinator(
            AddProjectCommand,
            RemoveProjectCommand,
            ToggleFavoriteCommand,
            RefreshCommand,
            _windowActions.LaunchCommand,
            _windowActions.ResumeCommand,
            _windowActions.InitCommand,
            _windowActions.UpdateCommand,
            _windowActions.VersionCommand,
            _actionBuilder,
            preferences.ShowOnlyProjectAgents);

        _projectPane.ProjectsChanged += OnProjectsChanged;

        RefreshProjects();

        _suppressFilterChange = true;
        ShowOnlyProjectAgents = preferences.ShowOnlyProjectAgents;
        _suppressFilterChange = false;

        _isPinned = preferences.PinLaunchWindow;
        _displayStyle = PathDisplayStyles.Parse(preferences.PathDisplayStyle);

        RefreshAgents();
    }

    /// <summary>
    ///   Registered projects shown in the Projects pane.
    /// </summary>
    public ObservableCollection<Project> Projects => _projectPane.Projects;

    /// <summary>
    ///   Agents shown in the Agents pane.
    /// </summary>
    public ObservableCollection<AgentItem> Agents => _agentPane.Agents;

    /// <summary>
    ///   Entries of the Projects pane's actions menu.
    /// </summary>
    public IReadOnlyList<MenuAction> ProjectsActions => _menuCoordinator.ProjectsActions;

    /// <summary>
    ///   Entries of the Agents pane's actions menu.
    /// </summary>
    public IReadOnlyList<MenuAction> AgentsActions => _menuCoordinator.AgentsActions;

    /// <summary>
    ///   The shared update control: current version when idle, update and restart actions when
    ///   an update is known.
    /// </summary>
    public UpdateControlViewModel UpdateControl { get; }

    /// <summary>
    ///   Window-level action commands: launch, settings, data folder, exit.
    /// </summary>
    public WindowActionCoordinator WindowActions => _windowActions;

    /// <summary>
    ///   The project that provides the launch context.
    /// </summary>
    public Project? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (!SetProperty(ref _selectedProject, value) || _suppressSelectionChange || value is null)
            {
                return;
            }

            _projectPane.Select(value);
            _statusCoordinator.ReportProjectSelected(value.Name);
            RefreshAgents();
        }
    }

    /// <summary>
    ///   The agent row the pane actions apply to.
    /// </summary>
    public AgentItem? SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (!SetProperty(ref _selectedAgent, value) || value is null)
            {
                return;
            }

            _agentPane.Select(value);
            _statusCoordinator.ReportAgentSelected(value.Name);
        }
    }

    /// <summary>
    ///   Whether the Agents pane hides agents that are not available in the project.
    /// </summary>
    public bool ShowOnlyProjectAgents
    {
        get => _showOnlyProjectAgents;
        set
        {
            if (!SetProperty(ref _showOnlyProjectAgents, value) || _suppressFilterChange)
            {
                return;
            }

            _preferencesStore.Update(preferences => preferences.ShowOnlyProjectAgents = value);

            RefreshAgents();
        }
    }

    /// <summary>
    ///   Whether the window stays visible when it loses focus. Persisted so the window keeps the
    ///   user's intent across restarts.
    /// </summary>
    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (!SetProperty(ref _isPinned, value))
            {
                return;
            }

            _preferencesStore.Update(preferences => preferences.PinLaunchWindow = value);

            _statusCoordinator.ReportWindowPinChanged(value);
        }
    }

    /// <summary>
    ///   How long project paths are shortened in the project rows.
    /// </summary>
    public PathDisplayStyle DisplayStyle
    {
        get => _displayStyle;
        private set => SetProperty(ref _displayStyle, value);
    }

    /// <summary>
    ///   Transient status or error text shown in the footer.
    /// </summary>
    public string StatusMessage => _statusCoordinator.CurrentMessage;

    /// <summary>
    ///   Adds a project folder via the folder picker.
    /// </summary>
    public RelayCommand AddProjectCommand { get; }

    /// <summary>
    ///   Removes the selected project from the registry.
    /// </summary>
    public RelayCommand RemoveProjectCommand { get; }

    /// <summary>
    ///   Toggles the favorite flag of the selected project.
    /// </summary>
    public RelayCommand ToggleFavoriteCommand { get; }

    /// <summary>
    ///   Clears detection and version caches and reloads both panes.
    /// </summary>
    public RelayCommand RefreshCommand { get; }

    /// <summary>
    ///   Opens the CLIHub data folder in Explorer.
    /// </summary>
    public RelayCommand OpenDataFolderCommand => _windowActions.OpenDataFolderCommand;

    /// <summary>
    ///   Opens the Settings window.
    /// </summary>
    public RelayCommand OpenSettingsCommand => _windowActions.OpenSettingsCommand;

    /// <summary>
    ///   Exits the application.
    /// </summary>
    public RelayCommand ExitCommand => _windowActions.ExitCommand;

    /// <summary>
    ///   Launches the selected agent in the current project.
    /// </summary>
    public RelayCommand LaunchCommand => _windowActions.LaunchCommand;

    /// <summary>
    ///   Resumes the selected agent in the current project.
    /// </summary>
    public RelayCommand ResumeCommand => _windowActions.ResumeCommand;

    /// <summary>
    ///   Initializes the selected agent in the current project.
    /// </summary>
    public RelayCommand InitCommand => _windowActions.InitCommand;

    /// <summary>
    ///   Updates the selected agent.
    /// </summary>
    public RelayCommand UpdateCommand => _windowActions.UpdateCommand;

    /// <summary>
    ///   Reports the selected agent's version in the status line.
    /// </summary>
    public RelayCommand VersionCommand => _windowActions.VersionCommand;

    /// <summary>
    ///   Launches the agent of the activated row.
    /// </summary>
    public RelayCommand<AgentItem> LaunchAgentCommand => _windowActions.LaunchAgentCommand;

    /// <summary>
    ///   Resumes the agent of the activated row.
    /// </summary>
    public RelayCommand<AgentItem> ResumeAgentCommand => _windowActions.ResumeAgentCommand;

    /// <summary>
    ///   Applies a path display style changed in Settings to the running window.
    /// </summary>
    public void ApplyPathDisplayStyle(PathDisplayStyle style) => DisplayStyle = style;

    private void OnStatusCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StatusMessageCoordinator.CurrentMessage))
        {
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    private void RefreshProjects()
    {
        _suppressSelectionChange = true;

        try
        {
            SetProperty(ref _selectedProject, _projectPane.Refresh());
        }
        finally
        {
            _suppressSelectionChange = false;
        }
    }

    private void RefreshAgents()
    {
        var hasAgents = _agentPane.Refresh(_projectPane.CurrentProject?.Path, ShowOnlyProjectAgents);
        SetProperty(ref _selectedAgent, _agentPane.SelectedAgent);

        if (!hasAgents)
        {
            _statusCoordinator.ReportNoAgentsFound();
        }
    }

    private void ShowWarning(string message) =>
        _notifications.ShowWarning(message);

    /// <summary>
    ///   Removes subscriptions owned by the launch-window composition model.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projectPane.ProjectsChanged -= OnProjectsChanged;
        _statusCoordinator.PropertyChanged -= OnStatusCoordinatorPropertyChanged;
    }

    private void OnProjectsChanged(object? sender, EventArgs e)
    {
        RefreshProjects();
        RefreshAgents();
    }
}

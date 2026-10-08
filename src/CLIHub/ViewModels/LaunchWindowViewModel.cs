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
    private readonly LaunchCommandCoordinator _launchCommandCoordinator;
    private readonly IPreferencesStore _preferencesStore;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IUserNotificationService _notifications;
    private readonly IApplicationLifetime _applicationLifetime;
    private readonly IExternalLauncher _externalLauncher;
    private readonly LaunchWindowActionBuilder _actionBuilder;

    private Project? _selectedProject;
    private AgentItem? _selectedAgent;
    private bool _showOnlyProjectAgents;
    private bool _isPinned;
    private PathDisplayStyle _displayStyle = PathDisplayStyles.Default;
    private bool _suppressSelectionChange;
    private bool _suppressFilterChange;
    private string _statusMessage = "CLIHub ready";
    private MenuAction? _filterAction;
    private bool _disposed;

    /// <summary>
    ///   Creates the view model with its services and loads projects and agents.
    /// </summary>
    /// <param name="projectPane"> Project-pane workflow boundary. </param>
    /// <param name="agentPane"> Agent-pane workflow boundary. </param>
    /// <param name="launchCommandCoordinator"> Agent-command workflow and result coordinator. </param>
    /// <param name="preferencesStore"> Store for persisted preferences. </param>
    /// <param name="updateControl"> Shared update control for this launch window. </param>
    /// <param name="settingsLauncher"> Settings window launcher. </param>
    /// <param name="notifications"> Information and warning notifications. </param>
    /// <param name="applicationLifetime"> Application lifetime control used by the Exit command. </param>
    /// <param name="externalLauncher"> Operating-system path launcher. </param>
    /// <param name="actionBuilder"> Builder for the pane Actions menus. </param>
    public LaunchWindowViewModel(
        ProjectPaneController projectPane,
        AgentPaneController agentPane,
        LaunchCommandCoordinator launchCommandCoordinator,
        IPreferencesStore preferencesStore,
        UpdateControlViewModel updateControl,
        ISettingsLauncher settingsLauncher,
        IUserNotificationService notifications,
        IApplicationLifetime applicationLifetime,
        IExternalLauncher externalLauncher,
        LaunchWindowActionBuilder actionBuilder)
    {
        _projectPane = projectPane;
        _agentPane = agentPane;
        _launchCommandCoordinator = launchCommandCoordinator;
        _preferencesStore = preferencesStore;
        _settingsLauncher = settingsLauncher;
        _notifications = notifications;
        _applicationLifetime = applicationLifetime;
        _externalLauncher = externalLauncher;
        _actionBuilder = actionBuilder;

        UpdateControl = updateControl;
        UpdateControl.OutcomeReported += OnUpdateOutcomeReported;

        AddProjectCommand = new RelayCommand(AddProject);
        RemoveProjectCommand = new RelayCommand(RemoveProject, () => SelectedProject != null);
        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite, () => SelectedProject != null);
        RefreshCommand = new RelayCommand(Refresh);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenSettingsCommand = new RelayCommand(() => _settingsLauncher.ShowSettings());
        ExitCommand = new RelayCommand(_applicationLifetime.Shutdown);

        LaunchCommand = new RelayCommand(() => _ = RunAgentCommandAsync(SelectedAgent, AgentCommandKind.Launch), HasSelectedAgent);
        ResumeCommand = new RelayCommand(() => _ = RunAgentCommandAsync(SelectedAgent, AgentCommandKind.Resume), HasSelectedAgent);
        InitCommand = new RelayCommand(() => _ = RunAgentCommandAsync(SelectedAgent, AgentCommandKind.Init), HasSelectedAgent);
        UpdateCommand = new RelayCommand(() => _ = RunAgentCommandAsync(SelectedAgent, AgentCommandKind.Update), HasSelectedAgent);
        VersionCommand = new RelayCommand(() => _ = RunAgentCommandAsync(SelectedAgent, AgentCommandKind.Version), HasSelectedAgent);

        LaunchAgentCommand = new RelayCommand<AgentItem>(
            item => _ = RunAgentCommandAsync(item, AgentCommandKind.Launch),
            item => item.CanLaunch);
        ResumeAgentCommand = new RelayCommand<AgentItem>(
            item => _ = RunAgentCommandAsync(item, AgentCommandKind.Resume),
            item => item.CanResume);

        _projectPane.ProjectsChanged += OnProjectsChanged;

        RefreshProjects();

        var preferences = _preferencesStore.Load();

        _suppressFilterChange = true;
        ShowOnlyProjectAgents = preferences.ShowOnlyProjectAgents;
        _suppressFilterChange = false;

        _isPinned = preferences.PinLaunchWindow;
        _displayStyle = PathDisplayStyles.Parse(preferences.PathDisplayStyle);

        BuildActions();

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
    public IReadOnlyList<MenuAction> ProjectsActions { get; private set; } = Array.Empty<MenuAction>();

    /// <summary>
    ///   Entries of the Agents pane's actions menu.
    /// </summary>
    public IReadOnlyList<MenuAction> AgentsActions { get; private set; } = Array.Empty<MenuAction>();

    /// <summary>
    ///   The shared update control: current version when idle, update and restart actions when
    ///   an update is known.
    /// </summary>
    public UpdateControlViewModel UpdateControl { get; }

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
            StatusMessage = $"Current project: {value.Name}";
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
            StatusMessage = $"Selected agent: {value.Name}";
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

            if (_filterAction is { } action && action.IsChecked != value)
            {
                action.IsChecked = value;
            }
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

            StatusMessage = value ? "Window pinned open" : "Window unpinned";
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
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

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

    /// <summary>
    ///   Applies a path display style changed in Settings to the running window.
    /// </summary>
    public void ApplyPathDisplayStyle(PathDisplayStyle style) => DisplayStyle = style;

    private bool HasSelectedAgent() => SelectedAgent is not null;

    private Task RunAgentCommandAsync(AgentItem? item, AgentCommandKind kind) =>
        _launchCommandCoordinator.RunAsync(
            item,
            _projectPane.CurrentProject,
            kind,
            message => StatusMessage = message,
            RefreshAgents);

    /// <summary>
    ///   Builds the per-pane actions menus from the existing commands, and wires the availability
    ///   filter entry to its persisted preference.
    /// </summary>
    private void BuildActions()
    {
        var actions = _actionBuilder.Build(
            AddProjectCommand,
            RemoveProjectCommand,
            ToggleFavoriteCommand,
            RefreshCommand,
            LaunchCommand,
            ResumeCommand,
            InitCommand,
            UpdateCommand,
            VersionCommand,
            ShowOnlyProjectAgents);
        ProjectsActions = actions.Projects;
        AgentsActions = actions.Agents;
        _filterAction = actions.FilterAction;

        _filterAction.PropertyChanged += OnFilterActionChanged;
    }

    /// <summary>
    ///   Mirrors the filter entry's checked state into the availability filter preference.
    /// </summary>
    private void OnFilterActionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MenuAction.IsChecked)
            && _filterAction is { } action
            && action.IsChecked != ShowOnlyProjectAgents)
        {
            ShowOnlyProjectAgents = action.IsChecked;
        }
    }

    private void AddProject()
    {
        try
        {
            var project = _projectPane.AddProject();
            if (project is null)
            {
                return;
            }

            RefreshProjects();
            StatusMessage = $"Added project: {project.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not add project: {ex.Message}";
            ShowWarning($"Could not add project: {ex.Message}");
        }
    }

    private void RemoveProject()
    {
        if (SelectedProject is not { } project)
        {
            StatusMessage = NoProjectSelectedMessage;
            return;
        }

        if (!_projectPane.RemoveSelectedProject())
        {
            return;
        }

        RefreshProjects();
        StatusMessage = $"Removed project: {project.Name}";
        RefreshAgents();
    }

    private void ToggleFavorite()
    {
        if (SelectedProject is not { } project)
        {
            StatusMessage = NoProjectSelectedMessage;
            return;
        }

        _projectPane.ToggleFavorite();
        StatusMessage = project.IsFavorite
            ? $"Added {project.Name} to favorites"
            : $"Removed {project.Name} from favorites";
    }

    private void Refresh()
    {
        _agentPane.InvalidateCaches();
        RefreshProjects();
        RefreshAgents();
        StatusMessage = "Refreshed agents, versions and availability.";
    }

    private void OpenDataFolder()
    {
        var root = AppPaths.Root;

        try
        {
            _externalLauncher.Open(root);
            StatusMessage = $"Opened {root}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open {root}: {ex.Message}";
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
            StatusMessage = "No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\";
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
        UpdateControl.OutcomeReported -= OnUpdateOutcomeReported;
        if (_filterAction is not null)
        {
            _filterAction.PropertyChanged -= OnFilterActionChanged;
        }
    }

    private void OnProjectsChanged(object? sender, EventArgs e)
    {
        RefreshProjects();
        RefreshAgents();
    }

    private void OnUpdateOutcomeReported(object? sender, string message) => StatusMessage = message;
}

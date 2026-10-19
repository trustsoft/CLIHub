namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Helpers;
using CLIHub.Launching;
using CLIHub.Lifetime;
using CLIHub.Settings;

/// <summary>
///   State and commands for the launch window: the project and agent lists, the current
///   selection, the availability filter, the path display style, the pin state, the status
///   message, and every window action.
/// </summary>
public sealed class LaunchWindowViewModel : ObservableObject, IPathDisplayStyleTarget, IDisposable
{
    private const string NoProjectSelectedMessage = "Select a project first.";

    private readonly ProjectPaneController projectPane;
    private readonly AgentPaneController agentPane;
    private readonly IPreferencesStore preferencesStore;
    private readonly LaunchWindowActionBuilder actionBuilder;
    private readonly IUserNotificationService notifications;
    private readonly MenuActionCoordinator menuCoordinator;
    private readonly StatusMessageCoordinator statusCoordinator;
    private readonly WindowActionCoordinator windowActions;

    private ProjectModel? selectedProject;
    private AgentItem? selectedAgent;
    private bool isPinned;
    private PathDisplayStyle displayStyle = PathDisplayStyles.Default;
    private bool suppressSelectionChange;
    private bool suppressFilterChange;
    private bool disposed;

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
    /// <param name="processMonitor"> Agent process monitor for update safety checks. </param>
    public LaunchWindowViewModel(ProjectPaneController projectPane,
                                 AgentPaneController agentPane,
                                 LaunchCommandCoordinator launchCommandCoordinator,
                                 IPreferencesStore preferencesStore,
                                 LaunchWindowActionBuilder actionBuilder,
                                 UpdateControlViewModel updateControl,
                                 ISettingsLauncher settingsLauncher,
                                 IUserNotificationService notifications,
                                 IApplicationLifetime applicationLifetime,
                                 IExternalLauncher externalLauncher,
                                 StatusMessageCoordinator statusCoordinator,
                                 AgentProcessMonitor processMonitor)
    {
        this.projectPane = projectPane;
        this.agentPane = agentPane;
        this.preferencesStore = preferencesStore;
        this.actionBuilder = actionBuilder;
        this.notifications = notifications;
        this.statusCoordinator = statusCoordinator;

        this.statusCoordinator.PropertyChanged += this.OnStatusCoordinatorPropertyChanged;

        this.UpdateControl = updateControl;

        this.windowActions = new WindowActionCoordinator(
            launchCommandCoordinator,
            statusCoordinator,
            processMonitor,
            settingsLauncher,
            applicationLifetime,
            externalLauncher,
            () => this.SelectedAgent,
            this.ResolveLaunchContext,
            this.RefreshAgents);

        this.AddProjectCommand = this.projectPane.AddCommand;
        this.RemoveProjectCommand = this.projectPane.RemoveCommand;
        this.ToggleFavoriteCommand = this.projectPane.ToggleFavoriteCommand;
        this.RefreshCommand = this.projectPane.RefreshCommand;

        var preferences = this.preferencesStore.Load();

        this.menuCoordinator = new MenuActionCoordinator(
            this.AddProjectCommand,
            this.RemoveProjectCommand,
            this.ToggleFavoriteCommand,
            this.RefreshCommand,
            this.windowActions.LaunchCommand,
            this.windowActions.ResumeCommand,
            this.windowActions.UpdateCommand,
            this.windowActions.VersionCommand,
            this.actionBuilder,
            preferences.ShowOnlyProjectAgents);

        this.projectPane.ProjectsChanged += this.OnProjectsChanged;

        this.RefreshProjects();

        this.suppressFilterChange = true;
        this.ShowOnlyProjectAgents = preferences.ShowOnlyProjectAgents;
        this.suppressFilterChange = false;

        this.isPinned = preferences.PinLaunchWindow;
        this.displayStyle = PathDisplayStyles.Parse(preferences.PathDisplayStyle);

        this.RefreshAgents();
    }

    /// <summary>
    ///   Registered projects shown in the Projects pane.
    /// </summary>
    public ObservableCollection<ProjectModel> Projects => this.projectPane.Projects;

    /// <summary>
    ///   Agents shown in the Agents pane.
    /// </summary>
    public ObservableCollection<AgentItem> Agents => this.agentPane.Agents;

    /// <summary>
    ///   Entries of the Projects pane's actions menu.
    /// </summary>
    public IReadOnlyList<MenuAction> ProjectsActions => this.menuCoordinator.ProjectsActions;

    /// <summary>
    ///   Entries of the Agents pane's actions menu.
    /// </summary>
    public IReadOnlyList<MenuAction> AgentsActions => this.menuCoordinator.AgentsActions;

    /// <summary>
    ///   The shared update control: current version when idle, update and restart actions when
    ///   an update is known.
    /// </summary>
    public UpdateControlViewModel UpdateControl { get; }

    /// <summary>
    ///   Window-level action commands: launch, settings, data folder, exit.
    /// </summary>
    public WindowActionCoordinator WindowActions => this.windowActions;

    /// <summary>
    ///   The project row that provides the launch context.
    /// </summary>
    public ProjectModel? SelectedProject
    {
        get => this.selectedProject;
        set
        {
            if (!this.SetProperty(ref this.selectedProject, value) || this.suppressSelectionChange || value is null)
            {
                return;
            }

            this.projectPane.Select(value);
            this.statusCoordinator.ReportProjectSelected(value.Name);
            this.RefreshAgents();
        }
    }

    /// <summary>
    ///   The agent row the pane actions apply to.
    /// </summary>
    public AgentItem? SelectedAgent
    {
        get => this.selectedAgent;
        set
        {
            if (!this.SetProperty(ref this.selectedAgent, value) || value is null)
            {
                return;
            }

            this.agentPane.Select(value);
            this.statusCoordinator.ReportAgentSelected(value.Name);
        }
    }

    /// <summary>
    ///   Whether the Agents pane hides agents that are not available in the project.
    /// </summary>
    public bool ShowOnlyProjectAgents
    {
        get;
        set
        {
            if (!this.SetProperty(ref field, value) || this.suppressFilterChange)
            {
                return;
            }

            PreferenceSyncHelper.SyncPreference(
                this.preferencesStore,
                preferences => preferences.ShowOnlyProjectAgents = value,
                this.RefreshAgents);
        }
    }

    /// <summary>
    ///   Whether the window stays visible when it loses focus. Persisted so the window keeps the
    ///   user's intent across restarts.
    /// </summary>
    public bool IsPinned
    {
        get => this.isPinned;
        set
        {
            if (!this.SetProperty(ref this.isPinned, value))
            {
                return;
            }

            PreferenceSyncHelper.SyncPreference(
                this.preferencesStore,
                preferences => preferences.PinLaunchWindow = value,
                () => this.statusCoordinator.ReportWindowPinChanged(value));
        }
    }

    /// <summary>
    ///   How long project paths are shortened in the project rows.
    /// </summary>
    public PathDisplayStyle DisplayStyle
    {
        get => this.displayStyle;
        private set => this.SetProperty(ref this.displayStyle, value);
    }

    /// <summary>
    ///   Transient status or error text shown in the footer.
    /// </summary>
    public string StatusMessage => this.statusCoordinator.CurrentMessage;

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
    public RelayCommand OpenDataFolderCommand => this.windowActions.OpenDataFolderCommand;

    /// <summary>
    ///   Opens the Settings window.
    /// </summary>
    public RelayCommand OpenSettingsCommand => this.windowActions.OpenSettingsCommand;

    /// <summary>
    ///   Exits the application.
    /// </summary>
    public RelayCommand ExitCommand => this.windowActions.ExitCommand;

    /// <summary>
    ///   Launches the selected agent in the current project.
    /// </summary>
    public RelayCommand LaunchCommand => this.windowActions.LaunchCommand;

    /// <summary>
    ///   Resumes the selected agent in the current project.
    /// </summary>
    public RelayCommand ResumeCommand => this.windowActions.ResumeCommand;

    /// <summary>
    ///   Updates the selected agent.
    /// </summary>
    public RelayCommand UpdateCommand => this.windowActions.UpdateCommand;

    /// <summary>
    ///   Reports the selected agent's version in the status line.
    /// </summary>
    public RelayCommand VersionCommand => this.windowActions.VersionCommand;

    /// <summary>
    ///   Launches the agent of the activated row.
    /// </summary>
    public RelayCommand<AgentItem> LaunchAgentCommand => this.windowActions.LaunchAgentCommand;

    /// <summary>
    ///   Resumes the agent of the activated row.
    /// </summary>
    public RelayCommand<AgentItem> ResumeAgentCommand => this.windowActions.ResumeAgentCommand;

    /// <summary>
    ///   Applies a path display style changed in Settings to the running window.
    /// </summary>
    public void ApplyPathDisplayStyle(PathDisplayStyle style) => this.DisplayStyle = style;

    private void OnStatusCoordinatorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StatusMessageCoordinator.CurrentMessage))
        {
            this.OnPropertyChanged(nameof(this.StatusMessage));
        }
    }

    /// <summary>
    ///   Re-reads and re-enriches the project pane and restores the selection by project ID.
    ///   Also the enrichment trigger for the launch window being shown or activated, so folder
    ///   state and logos are current while the user looks at the window.
    /// </summary>
    public void RefreshProjects()
    {
        this.suppressSelectionChange = true;

        try
        {
            this.SetProperty(ref this.selectedProject, this.projectPane.Refresh());
        }
        finally
        {
            this.suppressSelectionChange = false;
        }
    }

    /// <summary>
    ///   Preselects the stored default agent when the window is shown. Does nothing when no default
    ///   is stored or the stored agent is not present.
    /// </summary>
    public void ApplyDefaultAgentSelection()
    {
        var defaultAgentId = this.preferencesStore.Load().DefaultAgentId;
        if (string.IsNullOrWhiteSpace(defaultAgentId))
        {
            return;
        }

        var match = this.Agents.FirstOrDefault(agent => agent.Plugin.Id == defaultAgentId);
        if (match is not null)
        {
            this.SelectedAgent = match;
        }
    }

    private void RefreshAgents()
    {
        var hasAgents = this.agentPane.Refresh(this.selectedProject?.Path, this.ShowOnlyProjectAgents);
        this.SetProperty(ref this.selectedAgent, this.agentPane.SelectedAgent);

        if (!hasAgents)
        {
            this.statusCoordinator.ReportNoAgentsFound();
        }
    }

    /// <summary>
    ///   Resolves the launch context from the selected row at command time: the selected model's
    ///   project ID is looked up through the project service, so commands never hold a shared
    ///   live record.
    /// </summary>
    private Project? ResolveLaunchContext() =>
            this.selectedProject is null ? null : this.projectPane.FindProject(this.selectedProject.Id);

    private void ShowWarning(string message) =>
        this.notifications.ShowWarning(message);

    /// <summary>
    ///   Removes subscriptions owned by the launch-window composition model.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.projectPane.ProjectsChanged -= this.OnProjectsChanged;
        this.statusCoordinator.PropertyChanged -= this.OnStatusCoordinatorPropertyChanged;
    }

    private void OnProjectsChanged(object? sender, EventArgs e)
    {
        this.RefreshProjects();
        this.RefreshAgents();
    }
}

namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

using Microsoft.Extensions.Logging;

using CLIHub;
using CLIHub.Core.Formatting;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;
using CLIHub.Core.Models;
using CLIHub.Themes;

/// <summary>
///   State and commands for the launch window: the project and agent lists, the current
///   selection, the availability filter, the path display style, the pin state, the status
///   message, and every window action.
/// </summary>
public sealed class LaunchWindowViewModel : ObservableObject
{
    private const string NoProjectMessage = "Select a project before running an agent command.";
    private const string NoAgentMessage = "Select an agent before running an agent command.";
    private const string NoProjectSelectedMessage = "Select a project first.";

    private static readonly string DefaultAgentLogoPath =
        Path.Combine(AppContext.BaseDirectory, "default-project.png");

    private readonly IProjectService _projectService;
    private readonly IPluginManager _pluginManager;
    private readonly IAgentCommandWorkflow _agentCommandWorkflow;
    private readonly IAgentDetectionService _agentDetectionService;
    private readonly IAgentVersionService _agentVersionService;
    private readonly ILogoCacheService _logoCacheService;
    private readonly IPreferencesStore _preferencesStore;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly PromptState _promptState;
    private readonly IProjectDialogService _projectDialogs;
    private readonly IUserNotificationService _notifications;
    private readonly IApplicationLifetime _applicationLifetime;
    private readonly ILogger<LaunchWindowViewModel> _logger;
    private CancellationTokenSource? _versionPopulationCts;
    private int _versionPopulationGeneration;

    private Project? _selectedProject;
    private AgentItem? _selectedAgent;
    private bool _showOnlyProjectAgents;
    private bool _isPinned;
    private PathDisplayStyle _displayStyle = PathDisplayStyles.Default;
    private bool _suppressSelectionChange;
    private bool _suppressFilterChange;
    private string _statusMessage = "CLIHub ready";
    private MenuAction? _filterAction;

    /// <summary>
    ///   Creates the view model with its services and loads projects and agents.
    /// </summary>
    /// <param name="projectService"> Project service backing the Projects pane. </param>
    /// <param name="pluginManager"> Plugin manager backing the Agents pane. </param>
    /// <param name="agentCommandWorkflow"> Workflow executing agent commands. </param>
    /// <param name="agentDetectionService"> Service detecting host and project availability. </param>
    /// <param name="agentVersionService"> Service resolving agent versions. </param>
    /// <param name="logoCacheService"> Persistent logo cache for project and agent logos. </param>
    /// <param name="preferencesStore"> Store for persisted preferences. </param>
    /// <param name="updateService"> Update service for the version text and update checks. </param>
    /// <param name="settingsLauncher"> Settings window launcher. </param>
    /// <param name="promptState"> Modal prompt tracker used to keep the window visible. </param>
    /// <param name="projectDialogs"> Project folder and confirmation dialogs. </param>
    /// <param name="notifications"> Information and warning notifications. </param>
    /// <param name="applicationLifetime"> Application lifetime control used by the Exit command. </param>
    /// <param name="logger"> Logger for unexpected agent command failures. </param>
    /// <param name="updateControlLogger"> Logger for unexpected update control failures. </param>
    public LaunchWindowViewModel(
        IProjectService projectService,
        IPluginManager pluginManager,
        IAgentCommandWorkflow agentCommandWorkflow,
        IAgentDetectionService agentDetectionService,
        IAgentVersionService agentVersionService,
        ILogoCacheService logoCacheService,
        IPreferencesStore preferencesStore,
        IUpdateService updateService,
        ISettingsLauncher settingsLauncher,
        PromptState promptState,
        IProjectDialogService projectDialogs,
        IUserNotificationService notifications,
        IApplicationLifetime applicationLifetime,
        ILogger<LaunchWindowViewModel> logger,
        ILogger<UpdateControlViewModel> updateControlLogger)
    {
        _projectService = projectService;
        _pluginManager = pluginManager;
        _agentCommandWorkflow = agentCommandWorkflow;
        _agentDetectionService = agentDetectionService;
        _agentVersionService = agentVersionService;
        _logoCacheService = logoCacheService;
        _preferencesStore = preferencesStore;
        _settingsLauncher = settingsLauncher;
        _promptState = promptState;
        _projectDialogs = projectDialogs;
        _notifications = notifications;
        _applicationLifetime = applicationLifetime;
        _logger = logger;

        UpdateControl = new UpdateControlViewModel(updateService, updateControlLogger);
        UpdateControl.OutcomeReported += (_, message) => StatusMessage = message;

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

        _projectService.ProjectsChanged += (_, _) => RefreshProjects();

        var preferences = _preferencesStore.Load();

        _suppressFilterChange = true;
        ShowOnlyProjectAgents = preferences.ShowOnlyProjectAgents;
        _suppressFilterChange = false;

        _isPinned = preferences.PinLaunchWindow;
        _displayStyle = PathDisplayStyles.Parse(preferences.PathDisplayStyle);

        BuildActions();

        RefreshProjects();
        RefreshAgents();
    }

    /// <summary>
    ///   Registered projects shown in the Projects pane.
    /// </summary>
    public ObservableCollection<Project> Projects { get; } = new();

    /// <summary>
    ///   Agents shown in the Agents pane.
    /// </summary>
    public ObservableCollection<AgentItem> Agents { get; } = new();

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

            _projectService.SetCurrentProject(value.Id);
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
        AsyncOperationRunner.RunAsync(
            $"{kind} {item?.Name ?? "agent command"}",
            () => ExecuteAsync(item, kind),
            _logger,
            message => StatusMessage = message);

    /// <summary>
    ///   Builds the per-pane actions menus from the existing commands, and wires the availability
    ///   filter entry to its persisted preference.
    /// </summary>
    private void BuildActions()
    {
        ProjectsActions = new MenuAction[]
        {
            new() { Label = "Add Project...", Glyph = IconGlyphs.Add, Command = AddProjectCommand },
            new() { Label = "Remove Project", Glyph = IconGlyphs.Delete, Command = RemoveProjectCommand },
            new() { Label = "Toggle Favorite", Glyph = IconGlyphs.FavoriteStar, Command = ToggleFavoriteCommand },
            new() { Label = "Refresh", Glyph = IconGlyphs.Refresh, Command = RefreshCommand }
        };

        _filterAction = new MenuAction
        {
            Label = "Only agents available in project",
            Glyph = IconGlyphs.Filter,
            IsCheckable = true,
            IsChecked = ShowOnlyProjectAgents
        };

        _filterAction.PropertyChanged += OnFilterActionChanged;

        AgentsActions = new MenuAction[]
        {
            new() { Label = "Launch", Glyph = IconGlyphs.Play, Command = LaunchCommand },
            new() { Label = "Resume Session", Glyph = IconGlyphs.Refresh, Command = ResumeCommand },
            new() { Label = "Initialize", Glyph = IconGlyphs.Initialize, Command = InitCommand },
            new() { Label = "Update", Glyph = IconGlyphs.Update, Command = UpdateCommand },
            new() { Label = "Show Version", Glyph = IconGlyphs.Version, Command = VersionCommand },
            MenuAction.Separator(),
            _filterAction,
            MenuAction.Separator(),
            new() { Label = "Refresh", Glyph = IconGlyphs.Refresh, Command = RefreshCommand }
        };
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
        using (_promptState.Begin())
        {
            var folderPath = _projectDialogs.SelectProjectFolder();
            if (folderPath is null)
            {
                return;
            }

            try
            {
                var project = _projectService.AddProject(folderPath);
                _projectService.SetCurrentProject(project.Id);
                StatusMessage = $"Added project: {project.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not add project: {ex.Message}";
                ShowWarning($"Could not add project: {ex.Message}");
            }
        }
    }

    private void RemoveProject()
    {
        if (SelectedProject is not { } project)
        {
            StatusMessage = NoProjectSelectedMessage;
            return;
        }

        using (_promptState.Begin())
        {
            var confirmed = _projectDialogs.ConfirmProjectRemoval(project.Name);

            if (!confirmed)
            {
                return;
            }
        }

        _projectService.RemoveProject(project.Id);
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

        _projectService.ToggleFavorite(project.Id);
        StatusMessage = project.IsFavorite
            ? $"Added {project.Name} to favorites"
            : $"Removed {project.Name} from favorites";
    }

    private void Refresh()
    {
        _logoCacheService.InvalidateAll();
        _agentVersionService.Invalidate();
        _agentDetectionService.Invalidate();
        RefreshProjects();
        RefreshAgents();
        StatusMessage = "Refreshed agents, versions and availability.";
    }

    private void OpenDataFolder()
    {
        var root = AppPaths.Root;

        try
        {
            Process.Start(new ProcessStartInfo(root) { UseShellExecute = true });
            StatusMessage = $"Opened {root}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open {root}: {ex.Message}";
        }
    }

    private async Task ExecuteAsync(AgentItem? item, AgentCommandKind kind)
    {
        if (item is null)
        {
            StatusMessage = NoAgentMessage;
            return;
        }

        var project = _projectService.GetCurrentProject();
        if (project is null)
        {
            StatusMessage = NoProjectMessage;
            return;
        }

        if (item.Plugin.Commands?.Get(kind) is null)
        {
            StatusMessage = $"{item.Name} does not support the {kind.ToString().ToLowerInvariant()} command.";
            return;
        }

        StatusMessage = $"{kind} {item.Name}...";

        var result = await _agentCommandWorkflow.ExecuteAsync(item.Plugin, project, kind);
        var error = result.Error ?? "no error details";

        if (kind == AgentCommandKind.Version)
        {
            if (result.Success)
            {
                StatusMessage = $"{item.Name} version: {result.Output}";
                ShowInfo(result.Output ?? string.Empty, $"{item.Name} version");
            }
            else
            {
                StatusMessage = $"{item.Name} version failed: {error}";
            }
        }
        else
        {
            StatusMessage = result.Success
                ? $"{kind} started for {item.Name}"
                : $"{kind} failed: {error}";
        }

        RefreshAgents();
    }

    private void RefreshProjects()
    {
        _suppressSelectionChange = true;

        try
        {
            var projects = _projectService.GetAllProjects();

            // Diff sync: the service mutates the same Project instances this collection holds,
            // so in-place changes flow through the model's property notifications. Only
            // membership changes touch the collection, which keeps row and selection identity.
            for (var i = Projects.Count - 1; i >= 0; i--)
            {
                if (projects.All(p => p.Id != Projects[i].Id))
                {
                    Projects.RemoveAt(i);
                }
            }

            foreach (var project in projects)
            {
                if (Projects.All(p => p.Id != project.Id))
                {
                    Projects.Add(project);
                }
            }

            var current = _projectService.GetCurrentProject();
            SelectedProject = current is null
                ? null
                : Projects.FirstOrDefault(p => p.Id == current.Id);
        }
        finally
        {
            _suppressSelectionChange = false;
        }
    }

    private void RefreshAgents()
    {
        _versionPopulationCts?.Cancel();
        _versionPopulationCts?.Dispose();
        _versionPopulationCts = new CancellationTokenSource();
        var generation = ++_versionPopulationGeneration;
        var cancellationToken = _versionPopulationCts.Token;

        var currentProject = _projectService.GetCurrentProject()?.Path;
        var entries = AgentListComposer.Compose(
            _pluginManager.GetAllPlugins(),
            _agentDetectionService,
            currentProject,
            ShowOnlyProjectAgents);

        var selectedPluginId = SelectedAgent?.Plugin.Id;

        var selected = AgentListSynchronizer.Synchronize(
            Agents,
            entries,
            DefaultAgentLogoPath,
            selectedPluginId);

        SelectedAgent = selected;

        if (Agents.Count == 0)
        {
            StatusMessage = "No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\";
        }
        else
        {
            _ = PopulateVersionsAsync(Agents.ToList(), generation, cancellationToken);
        }
    }

    private async Task PopulateVersionsAsync(
        IReadOnlyList<AgentItem> items,
        int generation,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.WhenAll(items.Select(async item =>
            {
                var version = await _agentVersionService.GetVersionAsync(item.Plugin, cancellationToken);
                if (!cancellationToken.IsCancellationRequested && generation == _versionPopulationGeneration)
                {
                    item.Version = version ?? "unknown";
                }
            }));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Agent version population failed: {ex}");
        }
    }

    private void ShowInfo(string message, string title) =>
        _notifications.ShowInformation(message, title);

    private void ShowWarning(string message) =>
        _notifications.ShowWarning(message);
}

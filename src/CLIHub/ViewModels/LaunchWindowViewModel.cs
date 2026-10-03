namespace CLIHub.ViewModels;

using CLIHub.Core.Formatting;
using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

/// <summary>
/// State and commands for the launch window: the project and agent lists, the current
/// selection, the availability filter, the path display style, the pin state, the status
/// message, and every window action.
/// </summary>
public sealed class LaunchWindowViewModel : ObservableObject
{
    private const string NoProjectMessage = "Select a project before running an agent command.";
    private const string NoAgentMessage = "Select an agent before running an agent command.";
    private const string NoProjectSelectedMessage = "Select a project first.";

    private readonly IProjectService _projectService;
    private readonly IPluginManager _pluginManager;
    private readonly IAgentCommandService _agentCommandService;
    private readonly IAgentDetectionService _agentDetectionService;
    private readonly IAgentVersionService _agentVersionService;
    private readonly IConfigService _configService;
    private readonly IUpdateService _updateService;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly PromptState _promptState;

    private Project? _selectedProject;
    private AgentItem? _selectedAgent;
    private bool _showOnlyProjectAgents;
    private bool _isPinned;
    private PathDisplayStyle _displayStyle = PathDisplayStyles.Default;
    private bool _suppressSelectionChange;
    private bool _suppressFilterChange;
    private string _statusMessage = "CLIHub ready";

    public LaunchWindowViewModel(
        IProjectService projectService,
        IPluginManager pluginManager,
        IAgentCommandService agentCommandService,
        IAgentDetectionService agentDetectionService,
        IAgentVersionService agentVersionService,
        IConfigService configService,
        IUpdateService updateService,
        ISettingsLauncher settingsLauncher,
        PromptState promptState)
    {
        _projectService = projectService;
        _pluginManager = pluginManager;
        _agentCommandService = agentCommandService;
        _agentDetectionService = agentDetectionService;
        _agentVersionService = agentVersionService;
        _configService = configService;
        _updateService = updateService;
        _settingsLauncher = settingsLauncher;
        _promptState = promptState;

        VersionText = $"v{_updateService.GetCurrentVersion()}";

        AddProjectCommand = new RelayCommand(AddProject);
        RemoveProjectCommand = new RelayCommand(RemoveProject, () => SelectedProject != null);
        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite, () => SelectedProject != null);
        RefreshCommand = new RelayCommand(Refresh);
        CheckForUpdatesCommand = new RelayCommand(() => _ = CheckForUpdatesAsync());
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenSettingsCommand = new RelayCommand(() => _settingsLauncher.ShowSettings());
        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

        LaunchCommand = new RelayCommand(() => _ = ExecuteAsync(SelectedAgent, AgentCommandKind.Launch), HasSelectedAgent);
        ResumeCommand = new RelayCommand(() => _ = ExecuteAsync(SelectedAgent, AgentCommandKind.Resume), HasSelectedAgent);
        InitCommand = new RelayCommand(() => _ = ExecuteAsync(SelectedAgent, AgentCommandKind.Init), HasSelectedAgent);
        UpdateCommand = new RelayCommand(() => _ = ExecuteAsync(SelectedAgent, AgentCommandKind.Update), HasSelectedAgent);
        VersionCommand = new RelayCommand(() => _ = ExecuteAsync(SelectedAgent, AgentCommandKind.Version), HasSelectedAgent);

        LaunchAgentCommand = new RelayCommand<AgentItem>(
            item => _ = ExecuteAsync(item, AgentCommandKind.Launch),
            item => item.CanLaunch);
        ResumeAgentCommand = new RelayCommand<AgentItem>(
            item => _ = ExecuteAsync(item, AgentCommandKind.Resume),
            item => item.CanResume);

        _projectService.ProjectsChanged += (_, _) => RefreshProjects();

        var preferences = _configService.Load().Preferences;

        _suppressFilterChange = true;
        ShowOnlyProjectAgents = preferences.ShowOnlyProjectAgents;
        _suppressFilterChange = false;

        _isPinned = preferences.PinLaunchWindow;
        _displayStyle = PathDisplayStyles.Parse(preferences.PathDisplayStyle);

        RefreshProjects();
        RefreshAgents();
    }

    /// <summary>Registered projects shown in the Projects pane.</summary>
    public ObservableCollection<Project> Projects { get; } = new();

    /// <summary>Agents shown in the Agents pane.</summary>
    public ObservableCollection<AgentItem> Agents { get; } = new();

    /// <summary>Current application version, formatted for the footer.</summary>
    public string VersionText { get; }

    /// <summary>The project that provides the launch context.</summary>
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

    /// <summary>The agent row the pane actions apply to.</summary>
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

    /// <summary>Whether the Agents pane hides agents that are not available in the project.</summary>
    public bool ShowOnlyProjectAgents
    {
        get => _showOnlyProjectAgents;
        set
        {
            if (!SetProperty(ref _showOnlyProjectAgents, value) || _suppressFilterChange)
            {
                return;
            }

            var config = _configService.Load();
            config.Preferences.ShowOnlyProjectAgents = value;
            _configService.Save(config);

            RefreshAgents();
        }
    }

    /// <summary>
    /// Whether the window stays visible when it loses focus. Persisted so the window keeps the
    /// user's intent across restarts.
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

            var config = _configService.Load();
            config.Preferences.PinLaunchWindow = value;
            _configService.Save(config);

            StatusMessage = value ? "Window pinned open" : "Window unpinned";
        }
    }

    /// <summary>How long project paths are shortened in the project rows.</summary>
    public PathDisplayStyle DisplayStyle
    {
        get => _displayStyle;
        private set => SetProperty(ref _displayStyle, value);
    }

    /// <summary>Transient status or error text shown in the footer.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand AddProjectCommand { get; }
    public RelayCommand RemoveProjectCommand { get; }
    public RelayCommand ToggleFavoriteCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand CheckForUpdatesCommand { get; }
    public RelayCommand OpenDataFolderCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public RelayCommand ExitCommand { get; }

    public RelayCommand LaunchCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public RelayCommand InitCommand { get; }
    public RelayCommand UpdateCommand { get; }
    public RelayCommand VersionCommand { get; }

    /// <summary>Launches the agent of the activated row.</summary>
    public RelayCommand<AgentItem> LaunchAgentCommand { get; }

    /// <summary>Resumes the agent of the activated row.</summary>
    public RelayCommand<AgentItem> ResumeAgentCommand { get; }

    /// <summary>
    /// Applies a path display style changed in Settings to the running window.
    /// </summary>
    public void ApplyPathDisplayStyle(PathDisplayStyle style) => DisplayStyle = style;

    private bool HasSelectedAgent() => SelectedAgent is not null;

    private void AddProject()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        using (_promptState.Begin())
        {
            if (dialog.ShowDialog(Application.Current?.MainWindow) != true)
            {
                return;
            }
        }

        try
        {
            var project = _projectService.AddProject(dialog.FolderName);
            _projectService.SetCurrentProject(project.Id);
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

        using (_promptState.Begin())
        {
            var confirmed = Confirm($"Remove \"{project.Name}\" from CLIHub?\n\nThe folder and its files are not deleted.");

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
        _agentVersionService.Invalidate();
        _agentDetectionService.Invalidate();
        RefreshAgents();
        StatusMessage = "Refreshed agents, versions and availability.";
    }

    private void OpenDataFolder()
    {
        var root = DirectoryInitializer.GetAppDataRoot();

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

    private async Task CheckForUpdatesAsync()
    {
        StatusMessage = "Checking for updates...";

        var result = await _updateService.CheckForUpdatesAsync();

        StatusMessage = result.Status switch
        {
            UpdateStatus.UpdateAvailable => $"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})",
            UpdateStatus.UpToDate => $"Up to date (v{result.CurrentVersion})",
            UpdateStatus.NotInstalled => "Updates apply to installed builds only.",
            _ => "Update check failed or timed out."
        };
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

        var result = await _agentCommandService.ExecuteAsync(item.Plugin, kind, project.Path);
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
            Projects.Clear();

            foreach (var project in _projectService.GetAllProjects())
            {
                Projects.Add(project);
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
        var currentProject = _projectService.GetCurrentProject()?.Path;
        var entries = AgentListComposer.Compose(
            _pluginManager.GetAllPlugins(),
            _agentDetectionService,
            currentProject,
            ShowOnlyProjectAgents);

        var items = new List<AgentItem>();

        foreach (var entry in entries)
        {
            items.Add(new AgentItem
            {
                Plugin = entry.Plugin,
                Name = entry.Plugin.Name,
                LogoPath = entry.Plugin.LogoPath,
                IsAvailable = entry.IsAvailable
            });
        }

        var selectedPluginId = SelectedAgent?.Plugin.Id;

        Agents.Clear();

        foreach (var item in items)
        {
            Agents.Add(item);
        }

        SelectedAgent = selectedPluginId is null
            ? null
            : Agents.FirstOrDefault(a => a.Plugin.Id == selectedPluginId);

        if (items.Count == 0)
        {
            StatusMessage = "No agents found. Add plugin.json files under %APPDATA%\\CLIHub\\plugins\\";
        }
        else
        {
            _ = PopulateVersionsAsync(items);
        }
    }

    private async Task PopulateVersionsAsync(IReadOnlyList<AgentItem> items)
    {
        await Task.WhenAll(items.Select(async item =>
        {
            var version = await _agentVersionService.GetVersionAsync(item.Plugin);
            item.Version = version ?? "unknown";
        }));
    }

    /// <summary>
    /// Shows a dialog owned by the launch window, so it appears above the always-on-top shell.
    /// </summary>
    private static MessageBoxResult Prompt(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
    {
        var owner = Application.Current?.MainWindow;

        return owner is null
            ? MessageBox.Show(message, title, buttons, image)
            : MessageBox.Show(owner, message, title, buttons, image);
    }

    private static bool Confirm(string message) =>
        Prompt(message, "CLIHub", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static void ShowInfo(string message, string title) =>
        _ = Prompt(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    private static void ShowWarning(string message) =>
        _ = Prompt(message, "CLIHub", MessageBoxButton.OK, MessageBoxImage.Warning);
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CLIHub.App.Platform;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.App.ViewModels;

public sealed class PopupViewModel : INotifyPropertyChanged
{
    private readonly ProjectRegistry _registry;
    private readonly SettingsStore _settings;
    private readonly IReadOnlyList<AgentPlugin> _plugins;
    private readonly AgentDetector _detector;
    private readonly LauncherCore _launcher;
    private readonly LogoResolver _logoResolver;
    private readonly LogoImageService _logoImages;
    private readonly Func<string?> _pickFolder;
    private readonly Func<string, bool> _confirm;
    private readonly Action<Action> _postToUi;
    private readonly RelayCommand _removeProjectCommand;

    private ProjectItemViewModel? _selectedProject;
    private string _statusText = string.Empty;

    public PopupViewModel(
        ProjectRegistry registry,
        SettingsStore settings,
        IReadOnlyList<AgentPlugin> plugins,
        AgentDetector detector,
        LauncherCore launcher,
        LogoResolver logoResolver,
        LogoImageService logoImages,
        Func<string?> pickFolder,
        Func<string, bool> confirm,
        Action<Action> postToUi,
        string appVersion,
        Action openDataFolder)
    {
        _registry = registry;
        _settings = settings;
        _plugins = plugins;
        _detector = detector;
        _launcher = launcher;
        _logoResolver = logoResolver;
        _logoImages = logoImages;
        _pickFolder = pickFolder;
        _confirm = confirm;
        _postToUi = postToUi;

        AppVersion = appVersion;
        OpenDataFolderCommand = new RelayCommand(_ => openDataFolder());
        ExitCommand = new RelayCommand(_ => ExitRequested?.Invoke(this, EventArgs.Empty));

        foreach (var project in registry.Projects)
        {
            Projects.Add(CreateProjectItem(project));
        }

        _selectedProject = Projects.FirstOrDefault();

        AddProjectCommand = new RelayCommand(_ => AddProject());
        _removeProjectCommand = new RelayCommand(_ => RemoveProject(), _ => SelectedProject is not null);
        RemoveProjectCommand = _removeProjectCommand;

        RebuildAgents();
    }

    public ObservableCollection<ProjectItemViewModel> Projects { get; } = new();

    public ObservableCollection<AgentItemViewModel> Agents { get; } = new();

    public ICommand AddProjectCommand { get; }

    public ICommand RemoveProjectCommand { get; }

    public ICommand OpenDataFolderCommand { get; }

    public ICommand ExitCommand { get; }

    public string AppVersion { get; }

    public event EventHandler? CloseRequested;

    public event EventHandler? ExitRequested;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProjectItemViewModel? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (ReferenceEquals(_selectedProject, value))
            {
                return;
            }

            _selectedProject = value;
            OnPropertyChanged();
            _removeProjectCommand.RaiseCanExecuteChanged();
            RefreshAgentStates();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public void OnProbeRoundCompleted() => _postToUi(RebuildAgents);

    private void RebuildAgents()
    {
        Agents.Clear();

        foreach (var plugin in _plugins.Where(plugin => IsInstalled(plugin.Manifest)))
        {
            var logo = _logoImages.GetImage(_logoResolver.Resolve(plugin.Folder));
            Agents.Add(new AgentItemViewModel(plugin.Manifest, logo, Run));
        }

        RefreshAgentStates();

        if (_plugins.Count == 0)
        {
            StatusText = "Агенты не найдены в plugins/agents.";
        }
        else if (Agents.Count == 0)
        {
            StatusText = "Нет установленных агентов.";
        }
    }

    private ProjectItemViewModel CreateProjectItem(ProjectConfig project) =>
        new(project, _logoImages.GetImage(_logoResolver.ResolveProject(project)));

    private bool IsInstalled(AgentManifest manifest) =>
        _detector.GetHostStatus(manifest).HostInstalled;

    private void RefreshAgentStates()
    {
        var projectPath = SelectedProject?.Model.Path;

        foreach (var item in Agents)
        {
            var availability = _detector.GetAvailability(item.Manifest, projectPath);
            item.Update(canRun: availability.Run, version: _detector.GetHostStatus(item.Manifest).Version);
        }
    }

    private void AddProject()
    {
        var path = _pickFolder();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var result = _registry.Add(path);
        if (!result.Success || result.Project is null)
        {
            StatusText = result.Error ?? "Не удалось добавить проект.";
            return;
        }

        var item = CreateProjectItem(result.Project);
        Projects.Add(item);
        SelectedProject = item;
        StatusText = $"Добавлен проект: {result.Project.Name}";
    }

    private void RemoveProject()
    {
        var item = SelectedProject;
        if (item is null)
        {
            return;
        }

        if (!_confirm($"Удалить проект «{item.Name}» из списка?"))
        {
            return;
        }

        if (_registry.Remove(item.Model.Id ?? string.Empty))
        {
            Projects.Remove(item);
            SelectedProject = Projects.FirstOrDefault();
            StatusText = $"Удалён проект: {item.Name}";
        }
    }

    private void Run(AgentItemViewModel item)
    {
        var project = SelectedProject;
        if (string.IsNullOrWhiteSpace(project?.Model.Path))
        {
            StatusText = "Не выбран проект.";
            return;
        }

        var result = _launcher.Start(item.Manifest, "run", _settings.Runtime, project.Model.Path);
        if (!result.Success)
        {
            StatusText = result.Error ?? "Не удалось запустить агента.";
            return;
        }

        if (result.Warning is not null)
        {
            StatusText = result.Warning;
            return;
        }

        StatusText = $"Запущено: {item.Name}";
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

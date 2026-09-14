using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.App.ViewModels;

public sealed class PopupViewModel : INotifyPropertyChanged
{
    private readonly ProjectRegistry _registry;
    private readonly IReadOnlyList<AgentManifest> _manifests;
    private readonly AgentDetector _detector;
    private readonly LauncherCore _launcher;
    private readonly Func<string?> _pickFolder;
    private readonly Func<string, bool> _confirm;
    private readonly Action<Action> _postToUi;
    private readonly RelayCommand _removeProjectCommand;

    private ProjectConfig? _selectedProject;
    private string _statusText = string.Empty;

    public PopupViewModel(
        ProjectRegistry registry,
        IReadOnlyList<AgentManifest> manifests,
        AgentDetector detector,
        LauncherCore launcher,
        Func<string?> pickFolder,
        Func<string, bool> confirm,
        Action<Action> postToUi)
    {
        _registry = registry;
        _manifests = manifests;
        _detector = detector;
        _launcher = launcher;
        _pickFolder = pickFolder;
        _confirm = confirm;
        _postToUi = postToUi;

        foreach (var project in registry.Projects)
        {
            Projects.Add(project);
        }

        _selectedProject = Projects.FirstOrDefault();

        AddProjectCommand = new RelayCommand(_ => AddProject());
        _removeProjectCommand = new RelayCommand(_ => RemoveProject(), _ => SelectedProject is not null);
        RemoveProjectCommand = _removeProjectCommand;

        RebuildAgents();
    }

    public ObservableCollection<ProjectConfig> Projects { get; } = new();

    public ObservableCollection<AgentItemViewModel> Agents { get; } = new();

    public ICommand AddProjectCommand { get; }

    public ICommand RemoveProjectCommand { get; }

    public event EventHandler? CloseRequested;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProjectConfig? SelectedProject
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

        foreach (var manifest in _manifests.Where(IsInstalled))
        {
            Agents.Add(new AgentItemViewModel(manifest, Run));
        }

        RefreshAgentStates();

        if (_manifests.Count == 0)
        {
            StatusText = "Агенты не найдены в plugins/agents.";
        }
        else if (Agents.Count == 0)
        {
            StatusText = "Нет установленных агентов.";
        }
    }

    private bool IsInstalled(AgentManifest manifest) =>
        _detector.GetHostStatus(manifest).HostInstalled;

    private void RefreshAgentStates()
    {
        var projectPath = SelectedProject?.Path;

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

        Projects.Add(result.Project);
        SelectedProject = result.Project;
        StatusText = $"Добавлен проект: {result.Project.Name}";
    }

    private void RemoveProject()
    {
        var project = SelectedProject;
        if (project is null)
        {
            return;
        }

        if (!_confirm($"Удалить проект «{project.Name}» из списка?"))
        {
            return;
        }

        if (_registry.Remove(project.Id ?? string.Empty))
        {
            Projects.Remove(project);
            SelectedProject = Projects.FirstOrDefault();
            StatusText = $"Удалён проект: {project.Name}";
        }
    }

    private void Run(AgentItemViewModel item)
    {
        var project = SelectedProject;
        if (string.IsNullOrWhiteSpace(project?.Path))
        {
            StatusText = "Не выбран проект.";
            return;
        }

        var result = _launcher.Start(item.Manifest, "run", _registry.Runtime, project.Path);
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

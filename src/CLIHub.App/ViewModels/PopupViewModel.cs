using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CLIHub.Core.Models;
using CLIHub.Core.Services;

namespace CLIHub.App.ViewModels;

public sealed class PopupViewModel : INotifyPropertyChanged
{
    private readonly LauncherCore _launcher;
    private readonly string? _configRuntime;
    private ProjectConfig? _selectedProject;
    private string _statusText = string.Empty;

    public PopupViewModel(Config config, IReadOnlyList<AgentManifest> agents, LauncherCore launcher, string fallbackProjectPath)
    {
        _launcher = launcher;
        _configRuntime = config.Runtime;

        if (config.Projects.Count > 0)
        {
            foreach (var project in config.Projects)
            {
                Projects.Add(project);
            }
        }
        else
        {
            Projects.Add(new ProjectConfig { Name = DescribeFallbackProject(fallbackProjectPath), Path = fallbackProjectPath });
        }

        _selectedProject = Projects.FirstOrDefault();

        foreach (var agent in agents)
        {
            Agents.Add(new AgentItemViewModel(agent, Run));
        }

        if (Agents.Count == 0)
        {
            _statusText = "Агенты не найдены в plugins/agents.";
        }
    }

    public ObservableCollection<ProjectConfig> Projects { get; } = new();

    public ObservableCollection<AgentItemViewModel> Agents { get; } = new();

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

    private void Run(AgentItemViewModel item)
    {
        var project = SelectedProject;
        if (string.IsNullOrWhiteSpace(project?.Path))
        {
            StatusText = "Не выбран проект.";
            return;
        }

        var result = _launcher.Start(item.Manifest, "run", _configRuntime, project.Path);
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

    private static string DescribeFallbackProject(string path)
    {
        try
        {
            var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return string.IsNullOrWhiteSpace(name) ? path : name;
        }
        catch (ArgumentException)
        {
            return path;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

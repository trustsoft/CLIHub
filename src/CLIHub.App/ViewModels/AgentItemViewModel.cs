using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CLIHub.Core.Models;

namespace CLIHub.App.ViewModels;

public sealed class AgentItemViewModel : INotifyPropertyChanged
{
    private readonly RelayCommand _runCommand;
    private bool _canRun;
    private string? _version;

    public AgentItemViewModel(AgentManifest manifest, Action<AgentItemViewModel> run)
    {
        Manifest = manifest;
        _runCommand = new RelayCommand(_ => run(this), _ => CanRun);
    }

    public AgentManifest Manifest { get; }

    public string Name => Manifest.Name ?? Manifest.Id ?? "(без имени)";

    public bool CanRun
    {
        get => _canRun;
        private set
        {
            if (_canRun == value)
            {
                return;
            }

            _canRun = value;
            OnPropertyChanged();
            _runCommand.RaiseCanExecuteChanged();
        }
    }

    public string? Version
    {
        get => _version;
        private set
        {
            if (_version == value)
            {
                return;
            }

            _version = value;
            OnPropertyChanged();
        }
    }

    public ICommand RunCommand => _runCommand;

    public void Update(bool canRun, string? version)
    {
        CanRun = canRun;
        Version = version;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

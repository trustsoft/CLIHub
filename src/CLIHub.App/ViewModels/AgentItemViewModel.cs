using System.Windows.Input;
using CLIHub.Core.Models;

namespace CLIHub.App.ViewModels;

public sealed class AgentItemViewModel
{
    public AgentItemViewModel(AgentManifest manifest, Action<AgentItemViewModel> run)
    {
        Manifest = manifest;
        RunCommand = new RelayCommand(_ => run(this), _ => CanRun);
    }

    public AgentManifest Manifest { get; }

    public string Name => Manifest.Name ?? Manifest.Id ?? "(без имени)";

    public bool CanRun => Manifest.Actions is not null && Manifest.Actions.ContainsKey("run");

    public ICommand RunCommand { get; }
}

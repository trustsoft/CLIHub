using System.Windows.Media;
using CLIHub.Core.Models;

namespace CLIHub.App.ViewModels;

public sealed class ProjectItemViewModel
{
    public ProjectItemViewModel(ProjectConfig model, ImageSource logo)
    {
        Model = model;
        Logo = logo;
    }

    public ProjectConfig Model { get; }

    public string? Name => Model.Name;

    public string? Path => Model.Path;

    public ImageSource Logo { get; }
}

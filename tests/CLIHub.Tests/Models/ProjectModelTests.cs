namespace CLIHub.Tests.Models;

using CLIHub.Core.Models;
using System.ComponentModel;

public class ProjectModelTests
{
    [Theory]
    [InlineData(nameof(Project.IsFavorite))]
    [InlineData(nameof(Project.LastUsed))]
    [InlineData(nameof(Project.LogoPath))]
    public void Setters_ChangeValue_RaisePropertyChanged(string propertyName)
    {
        var project = new Project { Id = "id", Name = "Name", Path = "C:\\p" };
        var raised = new List<string?>();
        project.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        switch (propertyName)
        {
            case nameof(Project.IsFavorite): project.IsFavorite = true; break;
            case nameof(Project.LastUsed): project.LastUsed = DateTime.UtcNow; break;
            case nameof(Project.LogoPath): project.LogoPath = "logo.png"; break;
        }

        Assert.Equal([propertyName], raised);
    }

    [Theory]
    [InlineData(nameof(Project.IsFavorite))]
    [InlineData(nameof(Project.LastUsed))]
    [InlineData(nameof(Project.LogoPath))]
    public void Setters_SameValue_DoNotRaise(string propertyName)
    {
        var project = new Project { Id = "id", Name = "Name", Path = "C:\\p", IsFavorite = true, LastUsed = new DateTime(2026, 1, 1), LogoPath = "logo.png" };
        var raised = 0;
        project.PropertyChanged += (_, _) => raised++;

        switch (propertyName)
        {
            case nameof(Project.IsFavorite): project.IsFavorite = true; break;
            case nameof(Project.LastUsed): project.LastUsed = new DateTime(2026, 1, 1); break;
            case nameof(Project.LogoPath): project.LogoPath = "logo.png"; break;
        }

        Assert.Equal(0, raised);
    }
}

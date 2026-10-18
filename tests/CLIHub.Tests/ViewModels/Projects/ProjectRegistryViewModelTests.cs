namespace CLIHub.ViewModels;

using System.IO;

using Moq;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Launching;

public class ProjectRegistryViewModelTests
{
    [Fact]
    public void Load_WithManualAndDiscoveredProjects_ListsManualFirstThenAlphabetical()
    {
        var projects = new List<Project>
        {
            Project("manual-zulu", "Zulu", ProjectSource.Manual),
            Project("discovered-beta", "Beta", ProjectSource.Discovered),
            Project("manual-alpha", "alpha", ProjectSource.Manual)
        };
        var service = CreateService(projects);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>());

        section.Load();

        Assert.Equal(
            new[] { "alpha", "Zulu", "Beta" },
            section.Projects.Select(item => item.Name));
        Assert.Equal("Manual", section.Projects[0].SourceLabel);
        Assert.Equal("Discovered", section.Projects[2].SourceLabel);
        Assert.Equal("M", section.Projects[0].SourceMark);
        Assert.Equal("D", section.Projects[2].SourceMark);
    }

    [Fact]
    public void Load_WithMissingFolder_MarksOnlyThatRow()
    {
        var projects = new List<Project>
        {
            Project("present", "Present", ProjectSource.Manual, AppContext.BaseDirectory),
            Project("missing", "Missing", ProjectSource.Manual, MissingPath())
        };
        var service = CreateService(projects);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>());

        section.Load();

        Assert.False(section.Projects.Single(item => item.Id == "present").IsFolderMissing);
        Assert.True(section.Projects.Single(item => item.Id == "missing").IsFolderMissing);
    }

    [Fact]
    public void Load_ComputesRegistryCountsAndFolderHealth()
    {
        var projects = new List<Project>
        {
            Project("manual-ok", "Manual OK", ProjectSource.Manual, ExistingFolder()),
            Project("manual-missing", "Manual Missing", ProjectSource.Manual, MissingPath()),
            Project("discovered", "Discovered", ProjectSource.Discovered, ExistingFolder())
        };
        var service = CreateService(projects);
        var section = new ProjectRegistryViewModel(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            Mock.Of<IExternalLauncher>());

        section.Load();

        Assert.Equal(2, section.ManualCount);
        Assert.Equal(1, section.DiscoveredCount);
        Assert.Equal(3, section.TotalCount);
        Assert.Equal(1, section.MissingFolderCount);
        Assert.True(section.HasMissingFolders);
        Assert.Equal("1 discovered · 2 manual", section.ProjectMixSummary);
        Assert.Equal("1 folder missing", section.FolderStatusSummary);
    }

    [Fact]
    public void Load_WithNoMissingFolders_LeavesFolderHealthEmpty()
    {
        var project = Project("manual", "Manual", ProjectSource.Manual, ExistingFolder());
        var section = new ProjectRegistryViewModel(
            CreateService([project]).Object,
            new Mock<IProjectDialogService>().Object,
            Mock.Of<IExternalLauncher>());

        section.Load();

        Assert.False(section.HasMissingFolders);
        Assert.Equal(0, section.MissingFolderCount);
        Assert.Equal(string.Empty, section.FolderStatusSummary);
    }

    [Fact]
    public void Load_WithMultipleMissingFolders_PluralizesSummary()
    {
        var projects = new List<Project>
        {
            Project("first", "First", ProjectSource.Manual, MissingPath()),
            Project("second", "Second", ProjectSource.Discovered, MissingPath())
        };
        var section = new ProjectRegistryViewModel(
            CreateService(projects).Object,
            new Mock<IProjectDialogService>().Object,
            Mock.Of<IExternalLauncher>());

        section.Load();

        Assert.Equal(2, section.MissingFolderCount);
        Assert.Equal("2 folders missing", section.FolderStatusSummary);
    }

    [Fact]
    public void RegisterCommand_WithExistingPath_RegistersManualProjectWithoutChangingCurrent()
    {
        var folder = ExistingFolder();
        var projects = new List<Project>();
        var added = Project("added", "Added", ProjectSource.Manual, folder);
        var service = CreateService(projects);
        service.Setup(x => x.AddProject(folder)).Callback(() => projects.Add(added)).Returns(added);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>())
        {
            NewProjectPath = folder
        };

        section.RegisterCommand.Execute(null);

        Assert.Equal(added.Id, Assert.Single(section.Projects).Id);
        Assert.Equal(string.Empty, section.NewProjectPath);
        Assert.Equal(1, section.ManualCount);
        Assert.Equal(1, section.TotalCount);
        service.Verify(x => x.SetCurrentProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RegisterCommand_WithAlreadyRegisteredPath_KeepsSingleRowAndReportsError()
    {
        var folder = ExistingFolder();
        var existing = Project("existing", "Existing", ProjectSource.Manual, folder);
        var service = CreateService([existing]);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>())
        {
            NewProjectPath = folder
        };
        section.Load();

        section.RegisterCommand.Execute(null);

        Assert.Single(section.Projects);
        Assert.True(section.HasError);
        service.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RegisterCommand_WithBlankPath_ShowsErrorAndAddsNothing()
    {
        var service = CreateService([]);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>());

        section.RegisterCommand.Execute(null);

        Assert.True(section.HasError);
        Assert.Empty(section.Projects);
        service.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RegisterCommand_WithMissingFolder_ShowsErrorAndAddsNothing()
    {
        var service = CreateService([]);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>())
        {
            NewProjectPath = MissingPath()
        };

        section.RegisterCommand.Execute(null);

        Assert.True(section.HasError);
        Assert.Contains("existing absolute", section.ErrorText);
        Assert.Empty(section.Projects);
        service.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void BrowseCommand_WithSelectedFolder_FillsPathField()
    {
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Add Project Folder")).Returns("C:\\Project");
        var section = new ProjectRegistryViewModel(CreateService([]).Object, dialogs.Object, Mock.Of<IExternalLauncher>());

        section.BrowseCommand.Execute(null);

        Assert.Equal("C:\\Project", section.NewProjectPath);
    }

    [Fact]
    public void BrowseCommand_WhenSelectionIsCancelled_LeavesPathUnchanged()
    {
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Add Project Folder")).Returns((string?)null);
        var section = new ProjectRegistryViewModel(CreateService([]).Object, dialogs.Object, Mock.Of<IExternalLauncher>())
        {
            NewProjectPath = "C:\\Typed"
        };

        section.BrowseCommand.Execute(null);

        Assert.Equal("C:\\Typed", section.NewProjectPath);
    }

    [Fact]
    public void OpenFolderCommand_WithExistingFolder_OpensProjectFolder()
    {
        var project = Project("manual", "Manual", ProjectSource.Manual, AppContext.BaseDirectory);
        var service = CreateService([project]);
        var launcher = new Mock<IExternalLauncher>(MockBehavior.Strict);
        launcher.Setup(x => x.Open(AppContext.BaseDirectory));
        var section = new ProjectRegistryViewModel(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            launcher.Object);
        section.Load();

        section.OpenFolderCommand.Execute(section.Projects.Single());

        launcher.Verify(x => x.Open(AppContext.BaseDirectory), Times.Once);
    }

    [Fact]
    public void OpenFolderCommand_WithMissingFolder_CannotExecute()
    {
        var project = Project("missing", "Missing", ProjectSource.Manual, MissingPath());
        var service = CreateService([project]);
        var section = new ProjectRegistryViewModel(
            service.Object,
            new Mock<IProjectDialogService>().Object,
            Mock.Of<IExternalLauncher>());
        section.Load();

        Assert.False(section.OpenFolderCommand.CanExecute(section.Projects.Single()));
    }

    [Fact]
    public void RemoveCommand_WhenConfirmed_RemovesProject()
    {
        var manual = Project("manual", "Manual", ProjectSource.Manual);
        var projects = new List<Project> { manual };
        var service = CreateService(projects);
        service.Setup(x => x.RemoveProject("manual")).Callback(() => projects.Remove(manual));
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.ConfirmProjectRemoval("Manual")).Returns(true);
        var section = new ProjectRegistryViewModel(service.Object, dialogs.Object, Mock.Of<IExternalLauncher>());
        section.Load();

        section.RemoveCommand.Execute(section.Projects.Single());

        Assert.Empty(section.Projects);
        Assert.Equal(0, section.TotalCount);
        service.Verify(x => x.RemoveProject("manual"), Times.Once);
    }

    [Fact]
    public void RemoveCommand_WhenCancelled_KeepsProject()
    {
        var manual = Project("manual", "Manual", ProjectSource.Manual);
        var service = CreateService([manual]);
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.ConfirmProjectRemoval("Manual")).Returns(false);
        var section = new ProjectRegistryViewModel(service.Object, dialogs.Object, Mock.Of<IExternalLauncher>());
        section.Load();

        section.RemoveCommand.Execute(section.Projects.Single());

        Assert.Single(section.Projects);
        service.Verify(x => x.RemoveProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RemoveCommand_WithDiscoveredProject_CannotExecute()
    {
        var discovered = Project("discovered", "Discovered", ProjectSource.Discovered);
        var service = CreateService([discovered]);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>());
        section.Load();

        Assert.False(section.RemoveCommand.CanExecute(section.Projects.Single()));
    }

    [Fact]
    public void ProjectsChanged_RebuildsRows()
    {
        var projects = new List<Project> { Project("manual", "Manual", ProjectSource.Manual) };
        var service = CreateService(projects);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>());
        section.Load();

        projects.Add(Project("discovered", "Discovered", ProjectSource.Discovered));
        service.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Equal(2, section.Projects.Count);
    }

    [Fact]
    public void Dispose_UnsubscribesFromProjectChanges()
    {
        var projects = new List<Project> { Project("manual", "Manual", ProjectSource.Manual) };
        var service = CreateService(projects);
        var section = new ProjectRegistryViewModel(service.Object, new Mock<IProjectDialogService>().Object, Mock.Of<IExternalLauncher>());
        section.Load();

        section.Dispose();
        projects.Add(Project("discovered", "Discovered", ProjectSource.Discovered));
        service.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Single(section.Projects);
    }

    private static Mock<IProjectService> CreateService(List<Project> projects)
    {
        var service = new Mock<IProjectService>(MockBehavior.Strict);
        service.Setup(x => x.GetAllProjects()).Returns(() => (IReadOnlyList<Project>)projects.ToArray());
        return service;
    }

    private static Project Project(string id, string name, ProjectSource source, string? path = null) => new()
    {
        Id = id,
        Name = name,
        Path = path ?? AppContext.BaseDirectory,
        Source = source
    };

    private static string ExistingFolder() =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));

    private static string MissingPath() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
}

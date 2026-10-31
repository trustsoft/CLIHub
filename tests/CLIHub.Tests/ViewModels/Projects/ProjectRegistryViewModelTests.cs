namespace CLIHub.ViewModels;

using System.IO;

using Moq;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Launching;

public class ProjectRegistryViewModelTests : IDisposable
{
    private readonly List<string> tempDirs = new();

    [Fact]
    public void Load_WithManualAndDiscoveredProjects_ListsManualFirstThenAlphabetical()
    {
        var projects = new List<Project>
        {
            Project("manual-zulu", "Zulu", ProjectSource.Manual),
            Project("discovered-beta", "Beta", ProjectSource.Discovered),
            Project("manual-alpha", "alpha", ProjectSource.Manual)
        };
        var section = this.CreateSection(CreateService(projects));

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
    public void Load_WithMissingFolder_FlagsOnlyThatRow()
    {
        var projects = new List<Project>
        {
            Project("present", "Present", ProjectSource.Manual, AppContext.BaseDirectory),
            Project("missing", "Missing", ProjectSource.Manual, MissingPath())
        };
        var section = this.CreateSection(CreateService(projects));

        section.Load();

        Assert.True(section.Projects.Single(item => item.Id == "present").IsFolderAvailable);
        Assert.False(section.Projects.Single(item => item.Id == "missing").IsFolderAvailable);
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
        var section = this.CreateSection(CreateService(projects));

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
        var section = this.CreateSection(CreateService([project]));

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
        var section = this.CreateSection(CreateService(projects));

        section.Load();

        Assert.Equal(2, section.MissingFolderCount);
        Assert.Equal("2 folders missing", section.FolderStatusSummary);
    }

    [Fact]
    public void Load_SecondLoad_UpdatesRowsInPlace()
    {
        var folder = this.CreateTempDir();
        var projects = new List<Project>
        {
            Project("watched", "Watched", ProjectSource.Manual, folder)
        };
        var section = this.CreateSection(CreateService(projects));
        section.Load();
        var row = section.Projects.Single();
        Assert.True(row.IsFolderAvailable);

        Directory.Delete(folder);
        section.Load();

        Assert.Same(row, section.Projects.Single());
        Assert.False(row.IsFolderAvailable);
    }

    [Fact]
    public void RegisterCommand_WithExistingPath_RegistersManualProjectWithoutChangingCurrent()
    {
        var folder = ExistingFolder();
        var added = Project("added", "Added", ProjectSource.Manual, folder);
        var projects = new List<Project>();
        var service = CreateService(projects);
        service.Setup(x => x.AddProject(folder)).Callback(() => projects.Add(added)).Returns(added);
        var section = this.CreateSection(service);
        section.NewProjectPath = folder;

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
        var section = this.CreateSection(service);
        section.NewProjectPath = folder;
        section.Load();

        section.RegisterCommand.Execute(null);

        Assert.Single(section.Projects);
        Assert.True(section.HasError);
        service.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RegisterCommand_WithAlreadyRegisteredRootPath_ShowsDuplicateErrorWithoutAdding()
    {
        var existing = Project("root", "Root", ProjectSource.Manual, "C:");
        var service = CreateService([existing]);
        var section = this.CreateSection(service);
        section.NewProjectPath = "C:\\";
        section.Load();

        section.RegisterCommand.Execute(null);

        Assert.Single(section.Projects);
        Assert.True(section.HasError);
        Assert.Contains("already registered", section.ErrorText);
        service.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RegisterCommand_WithBlankPath_ShowsErrorAndAddsNothing()
    {
        var service = CreateService([]);
        var section = this.CreateSection(service);

        section.RegisterCommand.Execute(null);

        Assert.True(section.HasError);
        Assert.Empty(section.Projects);
        service.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RegisterCommand_WithMissingFolder_ShowsErrorAndAddsNothing()
    {
        var section = this.CreateSection(CreateService([]));
        section.NewProjectPath = MissingPath();

        section.RegisterCommand.Execute(null);

        Assert.True(section.HasError);
        Assert.Contains("existing absolute", section.ErrorText);
        Assert.Empty(section.Projects);
    }

    [Fact]
    public void BrowseCommand_WithSelectedFolder_FillsPathField()
    {
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Add Project Folder")).Returns("C:\\Project");
        var section = this.CreateSection(CreateService([]), dialogs: dialogs);

        section.BrowseCommand.Execute(null);

        Assert.Equal("C:\\Project", section.NewProjectPath);
    }

    [Fact]
    public void BrowseCommand_WhenSelectionIsCancelled_LeavesPathUnchanged()
    {
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Add Project Folder")).Returns((string?)null);
        var section = this.CreateSection(CreateService([]), dialogs: dialogs);
        section.NewProjectPath = "C:\\Typed";

        section.BrowseCommand.Execute(null);

        Assert.Equal("C:\\Typed", section.NewProjectPath);
    }

    [Fact]
    public void OpenFolderCommand_WithExistingFolder_OpensProjectFolder()
    {
        var project = Project("manual", "Manual", ProjectSource.Manual, AppContext.BaseDirectory);
        var launcher = new Mock<IExternalLauncher>(MockBehavior.Strict);
        launcher.Setup(x => x.Open(AppContext.BaseDirectory));
        var section = this.CreateSection(CreateService([project]), launcher: launcher);
        section.Load();

        section.OpenFolderCommand.Execute(section.Projects.Single());

        launcher.Verify(x => x.Open(AppContext.BaseDirectory), Times.Once);
    }

    [Fact]
    public void OpenFolderCommand_WithMissingFolder_CannotExecute()
    {
        var project = Project("missing", "Missing", ProjectSource.Manual, MissingPath());
        var section = this.CreateSection(CreateService([project]));
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
        var section = this.CreateSection(service, dialogs: dialogs);
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
        var section = this.CreateSection(service, dialogs: dialogs);
        section.Load();

        section.RemoveCommand.Execute(section.Projects.Single());

        Assert.Single(section.Projects);
        service.Verify(x => x.RemoveProject(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RemoveCommand_WithDiscoveredProject_CannotExecute()
    {
        var discovered = Project("discovered", "Discovered", ProjectSource.Discovered);
        var section = this.CreateSection(CreateService([discovered]));
        section.Load();

        Assert.False(section.RemoveCommand.CanExecute(section.Projects.Single()));
    }

    [Fact]
    public void ChangePath_ManualProject_CallsServiceAndReloads()
    {
        var newFolder = this.CreateTempDir();
        var manual = Project("manual", "Manual", ProjectSource.Manual);
        var projects = new List<Project> { manual };
        var service = CreateService(projects);
        service.Setup(x => x.ChangeProjectPath("manual", newFolder))
            .Callback(() =>
            {
                manual.Path = newFolder;
                manual.Name = Path.GetFileName(newFolder);
            });
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Change Project Folder")).Returns(newFolder);
        var section = this.CreateSection(service, dialogs: dialogs);
        section.Load();

        section.ChangePathCommand.Execute(section.Projects.Single());

        var row = section.Projects.Single();
        Assert.Equal(newFolder, row.Path);
        Assert.Equal(Path.GetFileName(newFolder), row.Name);
        Assert.True(row.IsFolderAvailable);
        Assert.False(section.HasError);
        service.Verify(x => x.ChangeProjectPath("manual", newFolder), Times.Once);
    }

    [Fact]
    public void ChangePath_UserCancelsDialog_NoServiceCall()
    {
        var manual = Project("manual", "Manual", ProjectSource.Manual);
        var service = CreateService([manual]);
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Change Project Folder")).Returns((string?)null);
        var section = this.CreateSection(service, dialogs: dialogs);
        section.Load();

        section.ChangePathCommand.Execute(section.Projects.Single());

        Assert.False(section.HasError);
        service.Verify(x => x.ChangeProjectPath(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ChangePath_ServiceThrowsDirectoryNotFound_DisplaysError()
    {
        var newFolder = this.CreateTempDir();
        var manual = Project("manual", "Manual", ProjectSource.Manual);
        var service = CreateService([manual]);
        service.Setup(x => x.ChangeProjectPath("manual", newFolder))
            .Throws(new DirectoryNotFoundException($"Folder does not exist: {newFolder}"));
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Change Project Folder")).Returns(newFolder);
        var section = this.CreateSection(service, dialogs: dialogs);
        section.Load();

        section.ChangePathCommand.Execute(section.Projects.Single());

        Assert.True(section.HasError);
        Assert.Equal($"The selected folder does not exist: Folder does not exist: {newFolder}", section.ErrorText);
    }

    [Fact]
    public void ChangePath_ServiceThrowsArgumentException_DisplaysError()
    {
        var newFolder = this.CreateTempDir();
        var manual = Project("manual", "Manual", ProjectSource.Manual);
        var service = CreateService([manual]);
        service.Setup(x => x.ChangeProjectPath("manual", newFolder))
            .Throws(new ArgumentException("The selected folder is already registered to a different project."));
        var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
        dialogs.Setup(x => x.SelectFolder("Change Project Folder")).Returns(newFolder);
        var section = this.CreateSection(service, dialogs: dialogs);
        section.Load();

        section.ChangePathCommand.Execute(section.Projects.Single());

        Assert.True(section.HasError);
        Assert.StartsWith("Could not change project path:", section.ErrorText);
        Assert.Contains("already registered to a different project", section.ErrorText);
    }

    [Fact]
    public void ChangePath_DiscoveredProject_CommandDisabled()
    {
        var discovered = Project("discovered", "Discovered", ProjectSource.Discovered);
        var section = this.CreateSection(CreateService([discovered]));
        section.Load();

        Assert.False(section.ChangePathCommand.CanExecute(section.Projects.Single()));
    }

    [Fact]
    public void ChangePath_ManualProject_CommandEnabled()
    {
        var manual = Project("manual", "Manual", ProjectSource.Manual);
        var section = this.CreateSection(CreateService([manual]));
        section.Load();

        Assert.True(section.ChangePathCommand.CanExecute(section.Projects.Single()));
    }

    [Fact]
    public void ProjectsChanged_UpdatesRowsInPlace()
    {
        var projects = new List<Project> { Project("manual", "Manual", ProjectSource.Manual) };
        var section = this.CreateSection(CreateService(projects));
        section.Load();
        var originalRow = section.Projects.Single();

        projects.Add(Project("discovered", "Discovered", ProjectSource.Discovered));
        section.Load();

        Assert.Equal(2, section.Projects.Count);
        Assert.Same(originalRow, section.Projects.Single(item => item.Id == "manual"));
    }

    [Fact]
    public void Dispose_UnsubscribesFromProjectChanges()
    {
        var projects = new List<Project> { Project("manual", "Manual", ProjectSource.Manual) };
        var service = CreateService(projects);
        var section = this.CreateSection(service);
        section.Load();

        section.Dispose();
        projects.Add(Project("discovered", "Discovered", ProjectSource.Discovered));
        service.Raise(x => x.ProjectsChanged += null, EventArgs.Empty);

        Assert.Single(section.Projects);
    }

    public void Dispose()
    {
        foreach (var dir in this.tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clihub-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        this.tempDirs.Add(dir);
        return dir;
    }

    private ProjectRegistryViewModel CreateSection(
        Mock<IProjectService> service,
        Mock<IProjectDialogService>? dialogs = null,
        Mock<IExternalLauncher>? launcher = null)
    {
        var cache = new Mock<ILogoCacheService>();
        var enricher = new ProjectEnricher(new ProjectLogoResolver(cache.Object, new LogoOptions(null)));

        return new ProjectRegistryViewModel(
            service.Object,
            enricher,
            new ProjectModelCatalog(),
            dialogs?.Object ?? new Mock<IProjectDialogService>().Object,
            launcher?.Object ?? Mock.Of<IExternalLauncher>());
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

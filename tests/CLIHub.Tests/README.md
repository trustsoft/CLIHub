# CLIHub.Tests Test Infrastructure

This file documents the `tests/CLIHub.Tests` project only. Repo-wide testing conventions — the naming rule,
Arrange/Act/Assert, mock strictness, hand-written fakes, time and temp-directory patterns, shared helpers, skip
semantics, architecture-boundary tests, and the gate commands — are in [`docs/testing.md`](../../docs/testing.md).

## Test Builders

Test builders provide fluent, readable APIs for creating complex test objects with sensible defaults. They eliminate boilerplate and make tests more maintainable.

### LaunchWindowViewModelBuilder

Located in `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`

The `LaunchWindowViewModel` takes eleven constructor dependencies (services, pane controllers, and coordinators). The builder provides:
- **Sensible defaults** - All dependencies are auto-mocked with reasonable behavior
- **Fluent configuration** - Override only what matters for your test
- **Two build modes**:
  - `Build()` - Returns just the ViewModel
  - `BuildWithMocks()` - Returns ViewModel + all mocks for verification

#### Example: Builder vs hand-wired setup

Before the builder existed, every test hand-wired the project pane, agent pane, update control, status
coordinator, command coordinator, process monitor, and finally `LaunchWindowViewModel` - roughly sixty
lines of setup repeated verbatim in each test, and it broke whenever a constructor changed. That
hand-wired shape is intentionally not reproduced here because it no longer matches the constructors.

**After** (focused and readable):

```csharp
[Fact]
public void AddProject_WhenFolderSelectionIsCancelled_UsesDialogService()
{
    var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
    dialogs.Setup(x => x.SelectProjectFolder()).Returns((string?)null);

    var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
        .WithProjectDialogService(dialogs)
        .BuildWithMocks();

    viewModel.AddProjectCommand.Execute(null);

    dialogs.Verify(x => x.SelectProjectFolder(), Times.Once);
    mocks.ProjectService.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
}
```

#### Available Configuration Methods

**Data configuration:**
- `WithProjects(params Project[])` - Set project catalog
- `WithCurrentProject(Project?)` - Set selected project
- `WithPlugins(params Plugin[])` - Set plugin catalog
- `WithPreferences(AppPreferences)` - Set application preferences

**Service mocks:**
- `WithProjectService(Mock<IProjectService>)` - Custom project service
- `WithPluginCatalog(Mock<IPluginCatalog>)` - Custom plugin catalog
- `WithProjectDialogService(Mock<IProjectDialogService>)` - Custom dialog service
- `WithUserNotificationService(Mock<IUserNotificationService>)` - Custom notification service
- `WithApplicationLifetime(Mock<IApplicationLifetime>)` - Custom lifetime manager
- `WithUpdateWorkflow(Mock<IUpdateWorkflow>)` - Custom update workflow

#### Build Modes

**Simple build** - when you don't need to verify mock interactions:

```csharp
var viewModel = new LaunchWindowViewModelBuilder()
    .WithProjects(project1, project2)
    .Build();

Assert.NotNull(viewModel);
Assert.Equal(2, viewModel.Projects.Count);
```

**Build with mocks** - when you need to verify service calls:

```csharp
var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
    .WithProjects(project1, project2)
    .BuildWithMocks();

viewModel.AddProjectCommand.Execute(null);

mocks.ProjectDialogService.Verify(x => x.SelectProjectFolder(), Times.Once);
```

## Test Organization

### Directory Structure

```
tests/
├── CLIHub.Core.Tests/          # Core logic tests, one folder per subsystem
│   ├── Agents/                 # Agent subsystem tests
│   ├── Configuration/          # Config serialization tests
│   ├── Fakes/                  # Shared fakes (FakeTimeProvider, FakeConfigurationRepository, ...)
│   ├── Infrastructure/         # Process, persistence, hotkey tests
│   ├── Models/                 # Domain model tests
│   ├── Plugins/                # Plugin loading tests
│   ├── Projects/               # Project management tests
│   └── Updates/                # Update workflow tests
│
├── CLIHub.Core.Windows.Tests/  # Windows adapter tests, flat layout with Fake*.cs doubles
│
└── CLIHub.Tests/               # WPF/ViewModel tests
    ├── Architecture/           # Architecture-boundary tests
    ├── Builders/               # Test builder infrastructure
    │   ├── LaunchWindowViewModelBuilder.cs
    │   └── LaunchWindowViewModelBuilderTests.cs
    ├── Support/                # TestHelpers (STA runner, repository root, operation lifetime)
    └── ViewModels/             # ViewModel behavior tests
```

## References

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [WPF documentation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)

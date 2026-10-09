# CLIHub Test Infrastructure

This document describes the testing infrastructure and patterns used in the CLIHub test suite.

## Overview

The CLIHub test suite consists of:
- **425 Core tests** - Testing UI-independent logic in `CLIHub.Core`
- **184 WPF tests** - Testing WPF-specific ViewModel and UI integration in `CLIHub`
- **Total: 609 tests**

## Test Builders

Test builders provide fluent, readable APIs for creating complex test objects with sensible defaults. They eliminate boilerplate and make tests more maintainable.

### LaunchWindowViewModelBuilder

Located in `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs`

The `LaunchWindowViewModel` requires 12+ dependencies. The builder provides:
- **Sensible defaults** - All dependencies are auto-mocked with reasonable behavior
- **Fluent configuration** - Override only what matters for your test
- **Two build modes**:
  - `Build()` - Returns just the ViewModel
  - `BuildWithMocks()` - Returns ViewModel + all mocks for verification

#### Example: Before and After

**Before** (verbose setup):

```csharp
[Fact]
public void AddProject_WhenFolderSelectionIsCancelled_UsesDialogService()
{
    var projectService = new Mock<IProjectService>();
    projectService.Setup(x => x.GetAllProjects()).Returns(Array.Empty<Project>());
    projectService.Setup(x => x.GetCurrentProject()).Returns((Project?)null);

    var pluginCatalog = new Mock<IPluginCatalog>();
    pluginCatalog.Setup(x => x.GetAllPlugins()).Returns(Array.Empty<Plugin>());

    var preferences = new Mock<IPreferencesStore>();
    preferences.Setup(x => x.Load()).Returns(new AppPreferences());

    var dialogs = new Mock<IProjectDialogService>(MockBehavior.Strict);
    dialogs.Setup(x => x.SelectProjectFolder()).Returns((string?)null);

    var notifications = new Mock<IUserNotificationService>(MockBehavior.Strict);
    var lifetime = new Mock<IApplicationLifetime>(MockBehavior.Strict);
    lifetime.Setup(x => x.Shutdown());
    var operationLifetime = new Mock<IApplicationOperationLifetime>();
    
    var projectPane = new ProjectPaneController(
        projectService.Object, dialogs.Object, new PromptState());
    var agentPane = new AgentPaneController(
        pluginCatalog.Object,
        new Mock<IAgentDetectionService>().Object,
        new Mock<IAgentVersionService>().Object,
        new Mock<ILogoCacheService>().Object,
        operationLifetime.Object);
    var updateControl = new UpdateControlViewModel(
        new Mock<IUpdateWorkflow>().Object,
        NullLogger<UpdateControlViewModel>.Instance,
        operationLifetime.Object);
    var statusCoordinator = new StatusMessageCoordinator(
        projectPane, agentPane, updateControl);
    var launchCoordinator = new LaunchCommandCoordinator(
        new Mock<IAgentCommandWorkflow>().Object,
        operationLifetime.Object,
        notifications.Object,
        NullLogger<LaunchCommandCoordinator>.Instance);
    var matcher = new AgentProcessMatcher(NullLogger<AgentProcessMatcher>.Instance);
    var processMonitor = new AgentProcessMonitor(
        new Mock<IAgentProcessInspector>().Object,
        matcher,
        pluginCatalog.Object,
        NullLogger<AgentProcessMonitor>.Instance);
    
    var viewModel = new LaunchWindowViewModel(
        projectPane,
        agentPane,
        launchCoordinator,
        preferences.Object,
        new LaunchWindowActionBuilder(),
        updateControl,
        new Mock<ISettingsLauncher>().Object,
        notifications.Object,
        lifetime.Object,
        new Mock<IExternalLauncher>().Object,
        statusCoordinator,
        processMonitor);

    viewModel.AddProjectCommand.Execute(null);

    dialogs.Verify(x => x.SelectProjectFolder(), Times.Once);
    projectService.Verify(x => x.AddProject(It.IsAny<string>()), Times.Never);
}
```

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
├── CLIHub.Core.Tests/          # Core logic tests (425 tests)
│   ├── Agents/                 # Agent subsystem tests
│   ├── Configuration/          # Config serialization tests
│   ├── Infrastructure/         # Process, persistence, hotkey tests
│   ├── Models/                 # Domain model tests
│   ├── Plugins/                # Plugin loading tests
│   ├── Projects/               # Project management tests
│   ├── Services/               # Service layer tests
│   └── Updates/                # Update workflow tests
│
└── CLIHub.Tests/               # WPF/ViewModel tests (184 tests)
    ├── Builders/               # Test builder infrastructure
    │   ├── LaunchWindowViewModelBuilder.cs
    │   └── LaunchWindowViewModelBuilderTests.cs
    ├── Fakes/                  # Fake implementations for testing
    └── ViewModels/             # ViewModel behavior tests
```

### Naming Conventions

Test methods follow the pattern: `MethodOrScenario_Condition_ExpectedResult`

Examples:
- `AddProject_WhenFolderSelectionIsCancelled_UsesDialogServiceAndDoesNotMutateProjects`
- `Build_WithDefaultConfiguration_CreatesValidViewModel`
- `BuildWithMocks_ReturnsViewModelAndAllMocks`

## Best Practices

### 1. Use Builders for Complex Objects

**Do:**
```csharp
var viewModel = new LaunchWindowViewModelBuilder()
    .WithProjects(project)
    .Build();
```

**Don't:**
```csharp
var viewModel = new LaunchWindowViewModel(
    new ProjectPaneController(...),
    new AgentPaneController(...),
    // ... 10 more dependencies
);
```

### 2. Only Mock What You Verify

**Do:**
```csharp
var dialogs = new Mock<IProjectDialogService>();
dialogs.Setup(x => x.SelectProjectFolder()).Returns(path);

var viewModel = new LaunchWindowViewModelBuilder()
    .WithProjectDialogService(dialogs)  // Only override this one
    .Build();
```

**Don't:**
```csharp
// Don't manually create all mocks when you only care about one
var projectService = new Mock<IProjectService>();
var pluginCatalog = new Mock<IPluginCatalog>();
var preferences = new Mock<IPreferencesStore>();
// ... etc
```

### 3. Use Fakes for Reusable Behavior

For test doubles needed across many tests, create a fake implementation in `tests/CLIHub.Tests/Fakes/`:

```csharp
public class FakeProjectService : IProjectService
{
    private readonly List<Project> _projects = new();
    
    public IReadOnlyList<Project> GetAllProjects() => _projects;
    // ... implement interface
}
```

Then use it consistently:
```csharp
var fakeService = new FakeProjectService();
var viewModel = new LaunchWindowViewModelBuilder()
    .WithProjectService(Mock.Get(fakeService))
    .Build();
```

### 4. Test One Behavior Per Test

**Do:**
```csharp
[Fact]
public void AddProject_CallsDialogService() { /* ... */ }

[Fact]
public void AddProject_AddsProjectWhenPathProvided() { /* ... */ }

[Fact]
public void AddProject_DoesNotAddProjectWhenCancelled() { /* ... */ }
```

**Don't:**
```csharp
[Fact]
public void AddProject_DoesEverything()
{
    // Tests 5 different behaviors in one test
}
```

## Running Tests

### All Tests
```powershell
dotnet test
```

### Specific Project
```powershell
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj
```

### Specific Test Class
```powershell
dotnet test --filter "FullyQualifiedName~LaunchWindowViewModelDialogTests"
```

### Specific Test Method
```powershell
dotnet test --filter "FullyQualifiedName~LaunchWindowViewModelDialogTests.AddProject_WhenFolderSelectionIsCancelled"
```

## Adding New Tests

### For Core Logic

1. Create test class in appropriate namespace under `tests/CLIHub.Core.Tests/`
2. Follow naming: `{ClassName}Tests.cs`
3. Use xUnit facts and theories
4. Keep tests isolated and fast

### For ViewModels

1. Create test class under `tests/CLIHub.Tests/ViewModels/`
2. Use the appropriate builder from `tests/CLIHub.Tests/Builders/`
3. Follow the existing patterns for mock verification
4. Test commands, property changes, and event subscriptions

## Future Improvements

Potential enhancements to the test infrastructure:

1. **More builders** - Create builders for other complex types:
   - `ProjectPaneControllerBuilder`
   - `AgentPaneControllerBuilder`
   - `SettingsViewModelBuilder`

2. **AutoFixture integration** - Consider using AutoFixture for automatic test data generation

3. **Snapshot testing** - For complex UI state verification

4. **Performance benchmarks** - Add BenchmarkDotNet for critical paths

5. **Integration tests** - End-to-end tests that exercise multiple layers together

## References

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [WPF Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-arranging-controls-on-windows-forms)

# Implementation Tasks

## Stage 1: Remove `PluginFeedClient.versionProvider` field

- [x] 1.1 Remove `private readonly IUpdateVersionProvider versionProvider;` field from `src/CLIHub.Core/Plugins/PluginFeedClient.cs:27`.
- [x] 1.2 Remove the `IUpdateVersionProvider versionProvider` parameter from the public constructor at line 37 (including the `<param name="versionProvider">` XML doc line at line 34).
- [x] 1.3 Remove the `IUpdateVersionProvider versionProvider` parameter from the internal test constructor at line 53 (including the `<param name="versionProvider">` XML doc line at line 49).
- [x] 1.4 Remove the `this.versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));` guard-assignment from the internal constructor body at line 57.
- [x] 1.5 Update the public constructor chain call (line 39) to pass one fewer argument: `this(CreateDefaultHandler(), preferencesStore, logger)`.
- [x] 1.6 Remove `using CLIHub.Core.Updates;` at `src/CLIHub.Core/Plugins/PluginFeedClient.cs:13` (the namespace is no longer referenced after removing the field and parameters).
- [x] 1.7 In `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs`, remove the `var versionProvider = Mock.Of<IUpdateVersionProvider>(...);` line at line 33.
- [x] 1.8 In the same file, remove the `versionProvider` argument from the `new PluginFeedClient(...)` call at line 34.
- [x] 1.9 In the same file, remove `using CLIHub.Core.Updates;` at line 14 (becomes unused after removing the Mock).
- [x] 1.10 Build: `dotnet build CLIHub.sln`
- [x] 1.11 Test: `dotnet test CLIHub.sln`

## Stage 2: Remove `BrowsePluginsDialog.viewModel` field

- [x] 2.1 Remove `private readonly BrowsePluginsDialogViewModel viewModel;` from `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs:12`.
- [x] 2.2 Remove `this.viewModel = viewModel;` from the constructor body at line 22.
- [x] 2.3 Confirm the constructor still contains `this.DataContext = viewModel;` (the parameter is kept) and the `Loaded` handler remains unchanged.
- [x] 2.4 Build: `dotnet build CLIHub.sln`
- [x] 2.5 Test: `dotnet test CLIHub.sln`

## Stage 3: Replace dead null-conditionals in `ApplicationHost.Shutdown()`

- [x] 3.1 In `src/CLIHub/Startup/ApplicationHost.cs`, replace `this.services?.GetService<IApplicationSession>()?.Dispose();` at line 101 with `this.services.GetService<IApplicationSession>()?.Dispose();` (direct access on `services`; keep `?.Dispose()` on the returned session).
- [x] 3.2 Replace `this.services?` on lines 110-111 with `this.services` so the expression reads `this.services.GetService<IApplicationOperationLifetime>()?.StopAsync(...)` (keep `?.StopAsync(...)` on the returned lifetime).
- [x] 3.3 Replace `this.services?.Dispose();` at line 121 with `this.services.Dispose();`.
- [x] 3.4 Build: `dotnet build CLIHub.sln`
- [x] 3.5 Test: `dotnet test CLIHub.sln`

## Stage 4: Remove `workingDirectory` parameter from `BuildInteractive`

- [x] 4.1 Remove `string workingDirectory` (parameter 3) from `WindowsCommandLineBuilder.BuildInteractive` in `src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs:29` and remove the corresponding `<param name="workingDirectory">` XML doc block at lines 17-18.
- [x] 4.2 Remove `string workingDirectory` (parameter 3) from `ICommandLineBuilder.BuildInteractive` in `src/CLIHub.Core/Processes/ICommandLineBuilder.cs:19` and remove the corresponding `<param name="workingDirectory">` XML doc line at line 15.
- [x] 4.3 Remove `string workingDirectory` (parameter 3) from `CommandLineBuilderAdapter.BuildInteractive` in `src/CLIHub.Core/Processes/CommandLineBuilderAdapter.cs:15` and remove it from the inner `WindowsCommandLineBuilder.BuildInteractive(...)` call at line 24.
- [x] 4.4 In `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:69-72`, remove `request.WorkingDirectory` from the `this.commandLineBuilder.BuildInteractive(runtime, request.Command, request.WorkingDirectory)` call. Confirm `request.WorkingDirectory` is still present at line 94 for `this.interactiveRunner.LaunchAsync`.
- [x] 4.5 In `tests/CLIHub.Core.Tests/Processes/CommandLineBuilderAdapterTests.cs`, remove the `@"C:\work"` argument from `this.adapter.BuildInteractive(...)` calls at lines 21, 36, 51, and 66.
- [x] 4.6 In `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs`, remove the `@"C:\work"` argument from `this.mockBuilder.Setup(x => x.BuildInteractive(...))` mock setups at lines 52, 95, and 120.
- [x] 4.7 In `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs`, remove the `shimDir` argument from `WindowsCommandLineBuilder.BuildInteractive(RuntimeKind.PowerShell, command, shimDir, ...)` at lines 137-138.
- [x] 4.8 In the same file, remove the `workingDirectory` argument from `WindowsCommandLineBuilder.BuildInteractive(runtimeKind, command, workingDirectory, ...)` at lines 177-178.
- [x] 4.9 Build: `dotnet build CLIHub.sln`
- [x] 4.10 Test: `dotnet test CLIHub.sln`

## Stage 5: Delete dead test helpers

- [x] 5.1 Delete the `private static SpacedPanel CreatePanel(params UIElement[] children)` overload from `tests/CLIHub.Tests/Controls/SpacedPanelTests.cs:182-186`. Confirm the other three `CreatePanel` overloads (lines 188-195, 197-204, 206-223) remain.
- [x] 5.2 Delete the `private record AnotherEvent(int Number) : IDomainEvent;` declaration from `tests/CLIHub.Core.Tests/Events/DomainEventBusTests.cs:10`.
- [x] 5.3 Build: `dotnet build CLIHub.sln`
- [x] 5.4 Test: `dotnet test CLIHub.sln`

## Stage 6: Remove unused `using` directives

- [x] 6.1 Remove `using CLIHub.Core.Infrastructure.Processes;` at `src/CLIHub.Core/Agents/AgentCommandService.cs:7`.
- [x] 6.2 Remove `using CLIHub.Core.Infrastructure.Processes;` at `src/CLIHub.Core/Agents/AgentVersionService.cs:9`.
- [x] 6.3 Remove `using CLIHub.Core.Infrastructure.Processes;` at `src/CLIHub/Composition/ServiceRegistration.cs:10`.
- [x] 6.4 Remove `using CLIHub.Core.Models;` at `src/CLIHub/ViewModels/Settings/WatchedDirectoryRow.cs:3`.
- [x] 6.5 Remove `using CLIHub.Theming;` at `src/CLIHub/ViewModels/Settings/AccentSelection.cs:4`.
- [x] 6.6 Remove `using System.Net;` at `src/CLIHub.Core/Plugins/PluginFeedClient.cs:3` (IDE0005 at HEAD), and verify the file no longer contains `using CLIHub.Core.Updates;` (removed in stage 1, step 1.6).
- [x] 6.6a Remove `using Moq;` at `tests/CLIHub.Core.Tests/Plugins/PluginFeedClientTests.cs:9` (unused after removing the version-provider mock).
- [x] 6.7 Build: `dotnet build CLIHub.sln`
- [x] 6.8 Test: `dotnet test CLIHub.sln`

## Stage 7: Final gates

- [x] 7.1 Build: `dotnet build CLIHub.sln`
- [x] 7.2 Test: `dotnet test CLIHub.sln`
- [x] 7.3 Validate: `openspec validate --all`
- [x] 7.4 Run `graphify update .` to refresh the knowledge graph.

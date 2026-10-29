## 1. Remove UpdateChecker class and UpdateService.checker field

- [x] 1.1 Delete `private readonly UpdateChecker checker;` at `src/CLIHub.Core/Updates/UpdateService.cs:32`.
- [x] 1.2 Delete `this.checker = new UpdateChecker(logger);` at `src/CLIHub.Core/Updates/UpdateService.cs:49` (public constructor).
- [x] 1.3 Delete `this.checker = new UpdateChecker(logger);` at `src/CLIHub.Core/Updates/UpdateService.cs:62` (private test constructor).
- [x] 1.4 Delete `tests/CLIHub.Core.Tests/Updates/UpdateCheckerTests.cs` (entire file, 136 lines).
- [x] 1.5 Delete `src/CLIHub.Core/Updates/UpdateChecker.cs` (entire file, 105 lines).
- [x] 1.6 Confirm `IUpdateChecker`/`IUpdateService`/`UpdateWorkflow` are untouched (`git diff --stat` shows no such files).
- [x] 1.7 Run `dotnet build CLIHub.sln` and confirm zero errors.
- [x] 1.8 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 2. Remove VelopackManagerProvider class

- [x] 2.1 Delete `tests/CLIHub.Core.Tests/Updates/VelopackManagerProviderTests.cs` (entire file).
- [x] 2.2 Delete `src/CLIHub.Core/Updates/VelopackManagerProvider.cs` (entire file, 69 lines).
- [x] 2.3 Run `dotnet build CLIHub.sln` and confirm zero errors.
- [x] 2.4 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 3. Remove trivial unused members

- [x] 3.1 Delete `src/CLIHub.Core/Models/Processes/ProcessCaptureResult.cs` (entire file, 10 lines; whole record is unreferenced).
- [x] 3.2 Delete the private `ValidatePlugin` method at `src/CLIHub.Core/Plugins/PluginCatalog.cs:138-151` (declaration through closing brace). Its logic is duplicated inline in `LoadPluginsCore`.
- [x] 3.3 Delete the private `ProbeTimeout` property at `src/CLIHub.Core/Agents/AgentVersionService.cs:133-140`. Leave `AgentReleaseClient.ProbeTimeout` (`AgentReleaseClient.cs:163`) alone — it is live.
- [x] 3.4 Delete `private const string NoProjectSelectedMessage = "Select a project first.";` at `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:21`.
- [x] 3.5 Run `dotnet build CLIHub.sln` and confirm zero errors and that the CA1823 warning for `NoProjectSelectedMessage` is gone.
- [x] 3.6 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 4. Remove StartupPreferencesApplier constructor parameter

The verified call sites are exactly one production registration and one test construction; the class is registered type-based, so DI adapts automatically.

- [x] 4.1 In `tests/CLIHub.Tests/Services/StartupPreferencesApplierTests.cs`, delete the now-unused `var preferencesStore = new Mock<IPreferencesStore>();` (line 18) and change the construction at line 21 to `new StartupPreferencesApplier(startupService.Object)`.
- [x] 4.2 Remove the `using CLIHub.Core.Configuration;` directive from that test file if `IPreferencesStore` was its only use (build must stay warning-free).
- [x] 4.3 In `src/CLIHub/Startup/StartupPreferencesApplier.cs`, delete the field (line 12), the `<param name="preferencesStore">` doc line (line 18), the `IPreferencesStore preferencesStore` constructor parameter (line 21), and the null guard (line 24).
- [x] 4.4 Update the constructor `<summary>`/`<param>` docs so they describe only the startup service.
- [x] 4.5 Run `dotnet build CLIHub.sln` and confirm zero errors.
- [x] 4.6 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 5. Remove StatusMessageCoordinator constructor parameter

Six construction sites: three in `StatusMessageCoordinatorTests.cs`, two in `LaunchWindowViewModelBuilder.cs`, one in `WindowActionCoordinatorTests.cs`. Each call passes `agentPane` between `projectPane` and `updateControl`.

- [x] 5.1 `tests/CLIHub.Tests/ViewModels/Shared/StatusMessageCoordinatorTests.cs:128` — drop the `agentPane` argument.
- [x] 5.2 `tests/CLIHub.Tests/ViewModels/Shared/StatusMessageCoordinatorTests.cs:171` — drop the `agentPane` argument.
- [x] 5.3 `tests/CLIHub.Tests/ViewModels/Shared/StatusMessageCoordinatorTests.cs:235` — drop the `agentPane` argument.
- [x] 5.4 `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:270-273` — drop the `agentPane,` line from the `new StatusMessageCoordinator(...)` call. Keep the `agentPane` local: it is still passed to `new LaunchWindowViewModel(...)`.
- [x] 5.5 `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:350-353` — same edit.
- [x] 5.6 `tests/CLIHub.Tests/ViewModels/Shared/WindowActionCoordinatorTests.cs:192` — drop the `agentPane` argument and delete the now-unused `agentPane`/`pluginCatalog` mock locals.
- [x] 5.7 In the three `StatusMessageCoordinatorTests` methods, delete the `agentPane` mock local and its now-unused `pluginCatalog` mock if they have no remaining use in that method.
- [x] 5.8 In `src/CLIHub/ViewModels/Shared/StatusMessageCoordinator.cs`, delete the field (line 10), the `<param name="agentPane">` doc line and its "currently unused, reserved for future events" sentence (line 19), the `AgentPaneController agentPane` constructor parameter (line 22), and the null guard (line 26).
- [x] 5.9 Run `dotnet build CLIHub.sln` and confirm zero errors.
- [x] 5.10 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 6. Remove AgentProcessFinder constructor parameter

Six construction sites: one production factory and five test sites.

- [x] 6.1 `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:53-56` — drop the `sp.GetRequiredService<IPluginCatalog>()` argument from the `new AgentProcessFinder(...)` factory lambda.
- [x] 6.2 `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs:38` — drop `new Mock<IPluginCatalog>().Object` from the constructor call.
- [x] 6.3 `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:287` — drop `pluginCatalog.Object` from the constructor call. Keep the `pluginCatalog` local: it is still passed to `AgentProcessMonitor`.
- [x] 6.4 `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:367` — same edit.
- [x] 6.5 `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:76` — drop `catalog.Object` from the constructor call; keep the local if it is still used elsewhere in the method.
- [x] 6.6 `tests/CLIHub.Tests/ViewModels/Shared/WindowActionCoordinatorTests.cs:131` — drop `catalog.Object` from the constructor call; keep the local if it is still used elsewhere.
- [x] 6.7 In `src/CLIHub.Core/Processes/AgentProcessFinder.cs`, delete the field (line 15), the `<param name="catalog">` doc line (line 22), the `IPluginCatalog catalog` constructor parameter (line 26), and the assignment (line 30).
- [x] 6.8 Remove the now-unused `using CLIHub.Core.Plugins;` from `AgentProcessFinder.cs` if nothing else in the file needs it.
- [x] 6.9 Run `dotnet build CLIHub.sln` and confirm zero errors and zero warnings.
- [x] 6.10 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 7. Remove UpdateDownloader.IsDownloading property and its test assertions

KEEP the `private bool isDownloading` field (`UpdateDownloader.cs:16`): it is the re-entrancy guard used by `DownloadAsync`. KEEP `UpdateService.IsDownloading`: different, live member.

- [x] 7.1 In `tests/CLIHub.Core.Tests/Updates/UpdateDownloaderTests.cs`, delete the `IsDownloading_Initially_ReturnsFalse` test including its `[Fact]` attribute (lines 98-103).
- [x] 7.2 Delete `Assert.False(this.downloader.IsDownloading);` at line 47 of the same file.
- [x] 7.3 Delete `Assert.False(this.downloader.IsDownloading);` at line 95 of the same file (re-verify the line after 7.1-7.2; anchor by text, not line number).
- [x] 7.4 Delete the public `IsDownloading` property and its doc comment at `src/CLIHub.Core/Updates/UpdateDownloader.cs:28-40`.
- [x] 7.5 Run `dotnet build CLIHub.sln` and confirm zero errors.
- [x] 7.6 Run `dotnet test CLIHub.sln` and confirm all tests pass, including `DownloadAsync_WhenConcurrent_ReturnsAlreadyDownloading`.

## 8. Remove IAgentManagementSection.IsCheckingUpdates interface member

- [x] 8.1 Delete the `IsCheckingUpdates` member and its doc comment at `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:118-121`.
- [x] 8.2 Confirm the concrete `AgentManagementViewModel.IsCheckingUpdates` (`AgentManagementViewModel.cs:201`) is untouched and still used by `CanDetect`/`CanCheckUpdates`/card logic.
- [x] 8.3 Run `dotnet build CLIHub.sln` and confirm zero errors.
- [x] 8.4 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 9. Final verification

- [x] 9.1 Run `dotnet build CLIHub.sln` and confirm zero warnings and zero errors.
- [x] 9.2 Run `dotnet test CLIHub.sln` and confirm all tests pass; record the passed/failed/skipped counts.
- [x] 9.3 Run `openspec validate --all` and confirm it passes.
- [x] 9.4 Run `git status --short` and confirm the changes are limited to the files named by this plan (plus this change directory).

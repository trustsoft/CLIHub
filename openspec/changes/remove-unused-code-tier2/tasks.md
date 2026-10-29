# Implementation Tasks

## Stage 1: Remove `ITrayHost` interface

- [x] 1.1 Delete `src/CLIHub/Tray/ITrayHost.cs`
- [x] 1.2 Remove `ITrayHost,` from `src/CLIHub/Tray/TrayIconController.cs:13` (keep `: IDisposable` and the implementation methods)
- [x] 1.3 Remove the `services.AddSingleton<ITrayHost>(sp => sp.GetRequiredService<TrayIconController>());` registration from `src/CLIHub/Composition/ServiceRegistration.cs:135`
- [x] 1.4 Remove `<c> ITrayHost </c>` from the XML documentation at `src/CLIHub/Composition/ServiceRegistration.cs:125`
- [x] 1.5 In `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs:158`, remove the `ITrayHost` assertion from the service registration collection check
- [x] 1.6 In `tests/CLIHub.Tests/Services/TrayMenuBuilderRegistrationTests.cs`, drop any `ITrayHost` descriptor assertions (keep `ITrayActions` assertions); rename test if its name mentions "host ports"
- [x] 1.7 Build: `dotnet build CLIHub.sln`
- [x] 1.8 Test: `dotnet test CLIHub.sln`

## Stage 2: Remove dead properties

- [x] 2.1 Delete the `AgentCardItem.HasCommands` property from `src/CLIHub/ViewModels/Agents/AgentCardItem.cs:121` (keep `CommandLabels` and `CommandsText`).
- [x] 2.2 Delete the `WindowActions` property from `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:152`.
- [x] 2.3 Delete the `LaunchCommand` property from the same file (line 284).
- [x] 2.4 Delete the `ResumeCommand` property from the same file (line 289).
- [x] 2.5 Delete the `VersionCommand` property from the same file (line 299).
- [x] 2.6 Delete the private `ShowWarning(string)` method from the same file (line 386).
- [x] 2.7 Confirm the kept members are still present: the `windowActions` field, `OpenDataFolderCommand`, `OpenSettingsCommand`, `ExitCommand`, `UpdateCommand`, `LaunchAgentCommand`, `ResumeAgentCommand`, `ApplyPathDisplayStyle`.
- [x] 2.8 Build: `dotnet build CLIHub.sln`
- [x] 2.9 Test: `dotnet test CLIHub.sln`

## Stage 3: Remove `IProcessOrchestrator.ResolveRuntimeAsync` chain

- [x] 3.1 Remove `ResolveRuntimeAsync` interface member from `src/CLIHub.Core/Processes/IProcessOrchestrator.cs:55` (including its XML doc, parameters, and return signature)
- [x] 3.2 Delete the `ResolveRuntimeAsync` implementation from `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:207-212` (keep `runtimeResolver` field and its direct usage at lines 58 and 122)
- [x] 3.3 Delete the `ResolveRuntimeAsync` method from `tests/CLIHub.Core.Tests/Fakes/FakeProcessOrchestrator.cs:37`
- [x] 3.4 Delete the `ResolveRuntimeAsync_DelegatesToResolver` test from `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs:318`
- [x] 3.5 Build: `dotnet build CLIHub.sln`
- [x] 3.6 Test: `dotnet test CLIHub.sln`

## Stage 4: Remove `IProjectService.AddDiscoveredProject` chain

- [x] 4.1 Remove `AddDiscoveredProject` interface member from `src/CLIHub.Core/Projects/IProjectService.cs:51` (keep `ReconcileDiscoveredProjects`)
- [x] 4.2 Delete the `AddDiscoveredProject` implementation from `src/CLIHub.Core/Projects/ProjectService.cs:99`
- [x] 4.3 Delete `AddDiscoveredProject_MarksDiscoveredSource` test from `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs` (starts line 299)
- [x] 4.4 Delete `AddDiscoveredProject_ExistingManual_DoesNotReclassify` test from `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs` (starts line 307)
- [x] 4.5 Build: `dotnet build CLIHub.sln`
- [x] 4.6 Test: `dotnet test CLIHub.sln`

## Stage 5: Remove `IRepositoryWatcher.Stop` chain

- [x] 5.1 Remove `Stop` interface member from `src/CLIHub.Core/Scanning/IRepositoryWatcher.cs:26` (keep `Dispose`)
- [x] 5.2 Delete the `Stop` implementation from `src/CLIHub.Core/Scanning/FileSystemRepositoryWatcher.cs:89`
- [x] 5.3 Delete the `Stop` method from `tests/CLIHub.Core.Tests/Fakes/ScanningFakes.cs:15` (it is present on `FakeRepositoryWatcher`).
- [x] 5.4 Build: `dotnet build CLIHub.sln`
- [x] 5.5 Test: `dotnet test CLIHub.sln`

## Stage 6: Remove two standalone dead properties

- [x] 6.1 Delete `AgentProcess.IsRuntimeHosted` property from `src/CLIHub.Core/Processes/AgentProcess.cs:48` (keep the `Runtime` property).
- [x] 6.2 Delete `GlobalHotkeyService.IsRegistered` property from `src/CLIHub/Hotkeys/GlobalHotkeyService.cs:28` (keep `registered` field)
- [x] 6.3 Build: `dotnet build CLIHub.sln`
- [x] 6.4 Test: `dotnet test CLIHub.sln`

## Stage 7: Remove interface-only members

- [x] 7.1 Remove `Token` from `src/CLIHub/Lifetime/IApplicationOperationLifetime.cs:11` (keep `ApplicationOperationLifetime.Token` and its test at `ApplicationOperationLifetimeTests.cs:26`)
- [x] 7.2 Remove `ShowOnPointerMonitor` from `src/CLIHub/Startup/IStartupWindow.cs:11` (keep `LaunchWindow.xaml.cs:65` implementation)
- [x] 7.3 Remove `SupportedAgentCount` and `HasDefaultAgent` from `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` (lines 26 and 46; keep concrete properties in `AgentManagementViewModel`)
- [x] 7.4 Remove `MissingFolderCount` from `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs:35` (keep concrete property in `ProjectRegistryViewModel`)
- [x] 7.5 Remove `Stop` from `src/CLIHub.Core/Scanning/IProjectScanner.cs:40` (keep `ProjectScanningService.Stop` and its tests at `ProjectScanningServiceTests.cs:133` and `:215`)
- [x] 7.6 Build: `dotnet build CLIHub.sln`
- [x] 7.7 Test: `dotnet test CLIHub.sln`

## Stage 8: Final gates

- [x] 8.1 Build: `dotnet build CLIHub.sln`
- [x] 8.2 Test: `dotnet test CLIHub.sln`
- [x] 8.3 Validate: `openspec validate --all`
- [x] 8.4 Remove the seven orphaned XML doc comments left behind by removed members: the `AgentCardItem.HasCommands` doc block, and the dangling `<inheritdoc />` on `ApplicationOperationLifetime.Token`, `ProjectScanningService.Stop`, `AgentManagementViewModel.SupportedAgentCount`, `AgentManagementViewModel.HasDefaultAgent`, `AgentManagementViewModel.IsCheckingUpdates` (orphaned since tier 1), and `ProjectRegistryViewModel.MissingFolderCount`.

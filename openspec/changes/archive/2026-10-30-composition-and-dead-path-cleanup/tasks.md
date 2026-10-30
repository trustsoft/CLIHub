# Tasks: composition-and-dead-path-cleanup

## Stage 1: Registration home + uniqueness/resolution tests

- [x] 1.1 Add `AddProcessServices` helper to `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` that registers `IRuntimeResolver`, `ICommandLineBuilder`, `IAgentProcessFinder`, `IProcessOrchestrator` with DI-constructed implementations (`AddSingleton<TInterface, TImpl>()`).
- [x] 1.2 Call `AddProcessServices()` from `AddClIHubCoreServices` in the same file.
- [x] 1.3 Delete the four duplicate registrations from `src/CLIHub/Composition/ServiceRegistration.cs:56-59`.
- [x] 1.4 Delete the four duplicate registrations from `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:47-63` (keep `IInteractiveProcessRunner`, `IProcessOutputRunner`, `IRuntimeExecutableLocator`, `IAgentProcessInspector`, `IStartupService`, and the guard registrations).
- [x] 1.5 Add `Assert.Single` descriptor uniqueness test in `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs` for `IRuntimeResolver`, `ICommandLineBuilder`, `IAgentProcessFinder`, `IProcessOrchestrator` over the composed WPF service collection.
- [x] 1.6 Add provider-resolution test in `ServiceRegistrationTests.cs`: resolve each of the four and assert the expected concrete type (`RuntimeResolver`, `CommandLineBuilderAdapter`, `AgentProcessFinder`, `ProcessOrchestrator`).
- [x] 1.7 Add an `AgentProcessMatcher` wiring assertion in `ServiceRegistrationTests.cs`: assert exactly one `AgentProcessMatcher` descriptor (singleton), assert the `IAgentProcessFinder` descriptor is `ImplementationType == typeof(AgentProcessFinder)` (so MS.DI injects its constructor dependencies rather than a factory constructing `new AgentProcessMatcher()`), and assert `Assert.Same` for two resolutions of `AgentProcessMatcher`. Do NOT add a production seam or use reflection to inspect the private field.
- [x] 1.8 Adjust `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs` if it asserts Windows registers the four; keep it green.
- [x] 1.9 Run UTF-8 safety check: `git diff` and confirm no mojibake (`â€`, `Ã`, `ï»¿`, `U+FFFD`).
- [x] 1.10 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (report exact pass/fail counts).

## Stage 2: Instance-check removal + startup docs

- [x] 2.1 Delete `src/CLIHub/Lifetime/IInstanceCoordinator.cs`.
- [x] 2.2 Delete `src/CLIHub/Lifetime/InstanceCoordinator.cs`.
- [x] 2.3 Delete `src/CLIHub/Lifetime/InstanceStatus.cs`.
- [x] 2.4 Remove `IInstanceCoordinator` dependency, argument, and check from `src/CLIHub/Startup/ApplicationBootstrapper.cs:13,31,39,55-59`.
- [x] 2.5 Remove `IInstanceCoordinator` registration from `src/CLIHub/Composition/ServiceRegistration.cs:84`.
- [x] 2.6 Delete `tests/CLIHub.Tests/InstanceCoordinatorTests.cs`.
- [x] 2.7 Remove `IInstanceCoordinator` assertions from `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs:33,95-96`.
- [x] 2.8 Update `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs`: drop the coordinator mock, delete the second-instance test, keep the ordering test intact, remove `InstanceStatus`/`Mock<IInstanceCoordinator>` usings.
- [x] 2.9 Update `docs/architecture/startup.md`: remove stage 2 (InstanceCoordinator), renumber stages 3-7 to 2-6, remove the "Signaling the first instance defensively during bootstrapper startup" failure-policy row, remove the InstanceCoordinator bullet from the implementation list.
- [x] 2.10 Check `docs/architecture.md` and `docs/project-context.md` for any claim about the defensive second check; update if present.
- [x] 2.11 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [x] 2.12 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (report exact pass/fail counts).

## Stage 3: Tray relay removal + tests

- [x] 3.1 Remove `UpdateDownloadRequested` event from `src/CLIHub/Tray/ITrayActions.cs`.
- [x] 3.2 Remove `UpdateDownloadRequested` event, subscription (`:31`), relay (`:55`), and unsubscribe (`:50`) from `src/CLIHub/Tray/TrayActions.cs`.
- [x] 3.3 Remove `UpdateDownloadRequested` event and its raise from `src/CLIHub/Tray/TrayCommandHandlers.cs:28,76`.
- [x] 3.4 Remove `showLaunchWindow` parameter from `TrayCommandHandlers.CreateCommands` (`:67,77`).
- [x] 3.5 Remove `IApplicationLifetime` dependency from `TrayCommandHandlers` constructor and field (`:23,50,60,78`).
- [x] 3.6 Update `TrayCommandHandlers.CreateCommands` to return `TrayMenuCommands` with inert default delegates for `RequestUpdateDownload`, `ShowLaunchWindow`, `Exit` (e.g., `static () => { }`).
- [x] 3.7 Update `TrayActions` constructor call to `CreateCommands` (`:32`) to remove the `showLaunchWindow` argument.
- [x] 3.8 Remove `TrayIconController` subscription (`:40`), relay (`:159`), and unsubscribe (`:144`) for the removed `actions.UpdateDownloadRequested`.
- [x] 3.9 Verify `TrayIconController.UpdateDownloadRequested` event and its menu lambda (`:131`) remain unchanged.
- [x] 3.10 Update `tests/CLIHub.Tests/Services/TrayActionsTests.cs`: remove `IApplicationLifetime` from the fixture and add a test asserting `TrayCommandHandlers.CreateCommands` returns inert defaults for the three controller-owned slots (`RequestUpdateDownload`, `ShowLaunchWindow`, `Exit`) so the ownership contract is pinned. Do NOT construct `TrayIconController` in a test (it creates a real `TaskbarIcon`/notify-icon and needs an STA host); the retained real path is already covered by `ShellCoordinatorTests.StartupUi_UpdateDownloadRequested_StartsDownloadWorkflow` and `ApplicationSessionTests`. Record this in the stage report.
- [x] 3.11 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [x] 3.12 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (report exact pass/fail counts).

## Stage 4: Settings placeholder + PageGroups

- [x] 4.1 Delete `src/CLIHub/Views/SettingsPlaceholderPage.xaml`.
- [x] 4.2 Delete `src/CLIHub/Views/SettingsPlaceholderPage.xaml.cs`.
- [x] 4.3 Remove the placeholder host line from `src/CLIHub/Views/SettingsWindow.xaml:477`.
- [x] 4.4 Remove `IsPlaceholderPageVisible` property and notifications from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:209,218,648`.
- [x] 4.5 Remove `IsImplemented` parameter from `SettingsPage` record in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:14,33`.
- [x] 4.6 Remove every `IsImplemented: true` positional argument from `SettingsPageCatalog.AllPagesData` in the same file (9 entries).
- [x] 4.7 Confirm `EmptyStateText` style has no other consumer (search: `rg "EmptyStateText" src/CLIHub`); if orphaned, remove the style definition from `src/CLIHub/Themes/SettingsStyles.xaml:154`.
- [x] 4.8 Delete `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:119-167` (three placeholder tests).
- [x] 4.9 Remove `IsImplemented` assertions from `SettingsNavigationTests.cs:71-72`.
- [x] 4.10 Delete `SettingsViewModel.PageGroups` from `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:187`.
- [x] 4.11 Delete `SettingsPageCatalog.PageGroups` and `PageGroupsData` from `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:124,137`.
- [x] 4.12 If `SettingsGroup` record (`:44`) becomes unreferenced, delete it; if still referenced, keep it.
- [x] 4.13 Update `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:25` for removed `PageGroups`.
- [x] 4.14 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [x] 4.15 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (report exact pass/fail counts).

## Stage 5: ApplyStartupUpdateCheck + AgentProcess.Runtime + ui docs/backlog

- [x] 5.1 Remove `ApplyStartupUpdateCheck` from `src/CLIHub/Settings/IPreferenceApplier.cs:30`.
- [x] 5.2 Remove `ApplyStartupUpdateCheck` implementation from `src/CLIHub/Settings/PreferenceApplier.cs:83-85`.
- [x] 5.3 Remove `ApplyStartupUpdateCheck` call from `src/CLIHub/Settings/SettingsApplicationService.cs:533`.
- [x] 5.4 Remove `ApplyStartupUpdateCheck` test setups from `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:975` and `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:71,154,213` (note: the file is under `Services/`, not `Settings/`).
- [x] 5.5 Remove `AgentProcess.Runtime` property and its `<remarks>` reference from `src/CLIHub.Core/Processes/AgentProcess.cs:34` plus the remarks at `:7-8,28-33`.
- [x] 5.6 Check `RuntimeExecutable` imports in `AgentProcess.cs`; remove the using if it becomes unused.
- [x] 5.8 Update `docs/architecture/ui.md`: remove `IsImplemented` mention from "Adding a Settings Page" step 2 (line 76) and the placeholder-fallback smoke step (lines 84-85).
- [x] 5.9 Add a `docs/backlog.md` entry under "UI and theming" (or the closest matching section) titled "Spec text still describes the removed placeholder and defensive instance paths", with: the three exact scenario names and spec files from `design.md` §Spec-text impact, severity Low, the evidence that they are now unreachable, the proposed remedy (amend or delete the scenarios in a later spec change), and the trigger "the next change that touches Settings page metadata or startup instance handling, or a request to amend the app-lifecycle/preferences-ui/settings-theme specs". Do not edit `openspec/specs/`.
- [x] 5.10 Run UTF-8 safety check: `git diff` and confirm no mojibake.
- [x] 5.11 Verify: `dotnet build CLIHub.sln` (expect 0 warnings, 0 errors) and `dotnet test CLIHub.sln` (report exact pass/fail counts).

## Stage 6: Final verification

- [x] 6.1 Run `dotnet build CLIHub.sln` and confirm 0 warnings, 0 errors.
- [x] 6.2 Run `dotnet test CLIHub.sln` and report exact final test counts (total passed, total failed).
- [x] 6.3 Run `openspec validate --all` and confirm all validations pass.
- [x] 6.4 Run `node .opencode/scripts/pipeline-state.mjs check` and confirm 10 ok.
- [x] 6.5 Run `git diff --check` and confirm no whitespace issues.
- [x] 6.6 Run `graphify update .` to keep the knowledge graph current.
- [x] 6.7 Report expected test count changes: tests drop by the removed placeholder/instance/relay tests and rise by the new uniqueness/resolution/identity tests. Confirm the net change matches the implementation.

## User smoke task (MANDATORY, not for the implementer)

**This task MUST be completed by the user before commit/archive approval.**

- [x] SMOKE.1 Start CLIHub.
- [x] SMOKE.2 Open the tray menu and exercise "Download and Install Update" (if available), "Show Window", and "Exit".
- [x] SMOKE.3 Launch a second instance and confirm the first instance activates (the launch window shows and focuses).
- [x] SMOKE.4 Open Settings and navigate to every page (General & Startup, Hotkeys & Quick Launcher, Projects, Project Scanning, CLI Agents, Terminal Profiles, Appearance, Logs, Updates).
- [x] SMOKE.5 Confirm each page shows its content (no placeholder state, no empty pages, no binding errors).
- [x] SMOKE.6 Launch an agent from the tray menu or the launch window.
- [x] SMOKE.7 Open Settings → CLI Agents and run "Detect Installed Agents" and "Check for Agent Updates".
- [x] SMOKE.8 Confirm no unexpected behavior, no crashes, no missing content.

**Commit and archive MUST wait for the user's explicit confirmation that the smoke task passed.**

## Notes

- UTF-8 safety: all edits via the edit tool, never PowerShell `Set-Content`/`Out-File`; UTF-8 without BOM; after each stage, run `git diff` and check for mojibake.
- Each stage ends with a green build/test; no stage may leave broken references or half-edited call sites.
- Final test counts: report exact numbers, do not force a specific count. The implementer's report should show the net change matches the removed and added tests.
- The user smoke task is mandatory and blocks commit/archive approval.

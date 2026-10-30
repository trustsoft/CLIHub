# Proposal: composition-and-dead-path-cleanup

## Why

The codebase has four categories of dead or redundant paths that increase surface area without adding value:

1. **Duplicate process-service registrations**: `IRuntimeResolver`, `ICommandLineBuilder`, `IAgentProcessFinder`, and `IProcessOrchestrator` are registered in both `WindowsServiceCollectionExtensions` (with explicit factories) and `ServiceRegistration` (with DI-constructed implementations). MS.DI last-wins semantics mean the Windows factories never run in the shipping app, so the registration ownership is misleading and the Windows test-composition path exercises a different graph than production.

2. **Unreachable instance check**: `ApplicationHost` acquires the single-instance guard before building the service provider and exits a duplicate process immediately (`src/CLIHub/Startup/ApplicationHost.cs:49-56`). The provider then owns the same guard. `ApplicationBootstrapper` repeats the check through `InstanceCoordinator.Coordinate()` (`:55-59`), which can only return `FirstInstance` in production because the guard's `IsFirstInstance` property is captured in its constructor. The defensive second check appears to implement a live failure path but never runs.

3. **Dead tray relay**: `TrayIconController` subscribes to `TrayActions.UpdateDownloadRequested` (`:40`), but `RefreshMenu` overwrites `RequestUpdateDownload`, `ShowLaunchWindow`, and `Exit` with its own delegates (`:129-134`). The real chain is the controller's own `UpdateDownloadRequested` event (`:23`) raised by the menu lambda and consumed by the shell coordinator. The handler-to-actions relay and its `IApplicationLifetime` dependency exist only to be overwritten.

4. **Tier-3 dead members**: after tier 1 and tier 2 cleanups, four unused paths remain:
   - The Settings placeholder page (`SettingsPlaceholderPage.xaml`) is unreachable because every `SettingsPage` entry passes `IsImplemented: true` (only tests construct `false`). `IsPlaceholderPageVisible` (`:209,218,648`) and the `IsImplemented` parameter (`:14,33`) exist only for an empty state that never shows.
   - `SettingsViewModel.PageGroups` (`:187`) and `SettingsPageCatalog.PageGroups`/`PageGroupsData` (`:124,137`) are never bound; XAML groups through `CollectionViewSource` over `Pages` (`SettingsWindow.xaml:252-256,338`).
   - `IPreferenceApplier.ApplyStartupUpdateCheck` (`:30`) only logs (`:83-85`); the persisted state remains the real read after the call site at `SettingsApplicationService.cs:533`.
   - `AgentProcess.Runtime` (`:34`) is never assigned and never read in production or tests.

This change removes all four categories as one behavior-preserving refactor.

## What changes

### Group 1: Process-service registration home

- Add a `AddProcessServices` helper to `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` that registers `IRuntimeResolver`, `ICommandLineBuilder`, `IAgentProcessFinder`, and `IProcessOrchestrator` with DI-constructed implementations (the same implementations currently registered in `ServiceRegistration.cs:56-59`), and call it from `AddClIHubCoreServices`.
- Delete the four duplicate registrations from `ServiceRegistration.cs:55-59` and from `WindowsServiceCollectionExtensions.cs:47-63`.
- Keep the Windows adapter registrations (`IInteractiveProcessRunner`, `IProcessOutputRunner`, `IRuntimeExecutableLocator`, `IAgentProcessInspector`, `IStartupService`, and the guard) in `AddWindowsInfrastructure`.
- Add `Assert.Single` descriptor uniqueness tests in `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs` for the four types over the composed WPF collection, plus provider-resolution assertions that resolve each of the four and verify the expected concrete type, plus a test that verifies `AgentProcessFinder` receives the same `AgentProcessMatcher` singleton (resolve `AgentProcessMatcher` and confirm the finder uses it).
- Adjust `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs` to keep it green.

The shipping graph stays identical: same implementations, same injected `AgentProcessMatcher` singleton, same singleton scope. Each of the four types has exactly one descriptor after the move.

### Group 2: Instance-check removal

- Delete `IInstanceCoordinator`, `InstanceCoordinator`, `InstanceStatus`, the bootstrapper dependency/argument/check, the registration at `ServiceRegistration.cs:84`, and `tests/CLIHub.Tests/InstanceCoordinatorTests.cs`.
- Remove the `IInstanceCoordinator` assertions from `ServiceRegistrationTests.cs:33,95-96`.
- Update `tests/CLIHub.Tests/Services/ApplicationBootstrapperTests.cs`: drop the coordinator mock, delete the second-instance test, keep the ordering test intact, and remove `InstanceStatus`/`Mock<IInstanceCoordinator>` usings.
- Update `docs/architecture/startup.md`: remove stage 2 (InstanceCoordinator), renumber the remaining stages, remove the "Signaling the first instance defensively during bootstrapper startup" failure-policy row, and remove the InstanceCoordinator bullet from the implementation list.
- Check `docs/architecture.md` and `docs/project-context.md` for any other claim about the defensive second check and update if present.

The host path remains exactly as it is: `ApplicationHost` still acquires the guard, exits a duplicate before any environment setup, and registers the guard into the provider for disposal. `IApplicationLifetime` MUST stay registered; `WindowActionCoordinator` still consumes it (`:21,43`).

### Group 3: Tray relay removal

- Delete `ITrayActions.UpdateDownloadRequested` event, `TrayActions` subscription/relay/unsubscribe, `TrayCommandHandlers.UpdateDownloadRequested` event and its raise, the `showLaunchWindow` parameter, and the `IApplicationLifetime` dependency from `TrayCommandHandlers`.
- Keep `TrayIconController.UpdateDownloadRequested` event and its menu lambda.
- Choose the cleanest shape for the controller-owned `TrayMenuCommands` slots in `TrayCommandHandlers.CreateCommands` (for example inert defaults that `RefreshMenu` replaces with `with`).
- Keep `ShowLaunchWindow` → controller and `Exit` → `Application.Current.Shutdown` in `RefreshMenu`.
- Update `tests/CLIHub.Tests/Services/TrayActionsTests.cs`: fixture no longer passes `IApplicationLifetime`; assert the retained real path (the controller's public event) rather than the removed relay; add a test if the removed relay had one.
- Do NOT rename the retained public event or change the shell-coordinator wiring in `src/CLIHub/Startup/ShellCoordinator.cs`.

### Group 4: Tier-3 dead members

1. **Placeholder page**: delete `SettingsPlaceholderPage.xaml/.xaml.cs`, the host line `src/CLIHub/Views/SettingsWindow.xaml:477`, `SettingsViewModel.IsPlaceholderPageVisible` (`:209,218,648`), the `IsImplemented` parameter on `SettingsPage` (`:14,33`), and every `true` positional argument in `SettingsPageCatalog.AllPagesData`. Delete the three placeholder tests in `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:119-167` and the `IsImplemented` assertions at `:71-72`. Remove the now-orphaned `EmptyStateText` style in `src/CLIHub/Themes/SettingsStyles.xaml:154` ONLY if repo-wide search proves it has no other consumer.

2. **PageGroups**: delete `SettingsViewModel.PageGroups` (`:187`), `SettingsPageCatalog.PageGroups`/`PageGroupsData` (`:124,137`), and the `SettingsGroup` record if it becomes unreferenced. Update `SettingsNavigationTests.cs:25`.

3. **ApplyStartupUpdateCheck**: delete `IPreferenceApplier.ApplyStartupUpdateCheck` (`:30`), its log-only implementation (`:83-85`), the call site after persistence (`SettingsApplicationService.cs:533`), and the four test setups (`SettingsViewModelApplicationTests.cs:975`, `SettingsApplicationServiceTests.cs:71,154,213`).

4. **AgentProcess.Runtime**: delete `AgentProcess.Runtime` (`:34` plus its `<remarks>` reference). Check `RuntimeExecutable` imports if they become unused.

Update `docs/architecture/ui.md`: remove the "Adding a Settings Page" step 2 mention of `IsImplemented` and the smoke step that mentions the placeholder fallback.

## Impact

- **Zero behavior change**: the tray menu labels/layout, update-download behavior, startup single-instance path, Settings navigation, process matching semantics, Settings save/scanning behavior, and the startup failure policy all remain exactly as they are.
- **Registration clarity**: each of the four process types has exactly one descriptor, and the Windows adapter tests compose the same graph as the WPF app through `AddClIHubCoreServices()` + `AddWindowsInfrastructure()`.
- **Surface reduction**: 1 interface, 1 enum, 2 classes, 1 record, 1 XAML page, 1 style, ~10 members, and 1 defensive startup stage removed. Tests drop by the removed placeholder/instance/relay tests and rise by the new uniqueness/wiring tests.
- **Documentation sync**: `startup.md` and `ui.md` no longer describe removed paths.

## Explicit non-goals

- No behavior change to the tray menu, update-download workflow, startup failure policy, Settings page navigation, process matching semantics, or Settings save/scanning behavior.
- Do not remove `IStartupWindow` (separate backlog entry).
- Do not consolidate test helpers or split the agent test file (portfolio item 7).
- Do not touch the `launch-window-composition` files unless the tree is dirty.
- No new abstractions, no production seams added for tests.

## Spec-text impact

This change removes three durable-spec scenarios but changes no runtime behavior:

- `openspec/specs/app-lifecycle/spec.md` — Requirement "Single instance resource ownership", scenario "Second instance exits through the provider lifecycle" (the provider-owned defensive check).
- `openspec/specs/preferences-ui/spec.md` — Requirement "Settings sections": "A page whose settings are not yet implemented SHALL show a neutral empty state", and scenario "Page without implemented settings".
- `openspec/specs/settings-theme/spec.md` — scenario "Placeholder page readable".

These statements become descriptive-only/unreachable after this change. They are not edited in this behavior-preserving refactor; reconciling them is parked in `docs/backlog.md` with a trigger: "the next change that touches Settings page metadata or startup instance handling, or a request to amend the lifecycle/preferences/settings-theme specs".

## Dependencies

- None. This change can be applied independently and is fully green when complete.

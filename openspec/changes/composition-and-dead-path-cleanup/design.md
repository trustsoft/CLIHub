# Design: composition-and-dead-path-cleanup

## Verified current state

All file:line references have been confirmed by the orchestrator against the working tree at HEAD `79f716d`.

### Group 1: Process-service registrations

- `src/CLIHub/Composition/ServiceRegistration.cs:52-59` calls `AddClIHubCoreServices` then `AddWindowsInfrastructure`, then re-registers `IRuntimeResolver`, `ICommandLineBuilder`, `IAgentProcessFinder`, `IProcessOrchestrator` at `:56-59` with DI-constructed types (`AddSingleton<TInterface, TImpl>()`).
- `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:47-63` registers the same four with explicit factories. Its `IAgentProcessFinder` factory uses `new AgentProcessMatcher()` (`:53-55`), whereas the app's last-wins registration constructs `AgentProcessFinder` through DI and injects the `AgentProcessMatcher` singleton registered in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:140`.
- MS.DI last-wins: in the shipping app, the Windows factories for these four never run. The Windows adapter test composes `AddClIHubCoreServices()` + `AddWindowsInfrastructure()` (`tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs:12-28`), so both registration sets are currently exercised somewhere.

### Group 2: Instance check

- `src/CLIHub/Startup/ApplicationHost.cs:49-56`: acquires the guard, exits a duplicate (SignalActivation + WpfApplicationLifetime.Shutdown) before any environment setup; the provider is then given the already-acquired guard (`:69`) and owns disposal.
- `src/CLIHub/Startup/ApplicationBootstrapper.cs:13,31,39,55-59`: takes `IInstanceCoordinator`, calls `Coordinate()`, returns early on `InstanceStatus.SecondInstance`.
- `src/CLIHub/Lifetime/IInstanceCoordinator.cs`, `InstanceCoordinator.cs` (Coordinate at `:26-35`), `InstanceStatus.cs` exist; registered at `ServiceRegistration.cs:84`.
- The guard's `IsFirstInstance` is captured in its constructor (`src/CLIHub.Core.Windows/SingleInstanceGuard.cs:16,57`), so the provider-owned guard is always the first instance by the time the bootstrapper runs. The defensive path can only return `FirstInstance` in production.

### Group 3: Tray relay

- `src/CLIHub/Tray/TrayIconController.cs`: subscribes `actions.UpdateDownloadRequested` (`:40`), overwrites `RequestUpdateDownload`, `ShowLaunchWindow`, `Exit` in `RefreshMenu` (`:129-134`), unsubscribes (`:144`), and relays (`:159`). The real chain is the controller's own `UpdateDownloadRequested` event (`:23`) raised by its menu lambda, consumed by the shell coordinator.
- `src/CLIHub/Tray/TrayActions.cs`: declares/raises `UpdateDownloadRequested` (`:16,31,50,55`) and relays the handler event; `CreateCommands` passes `static () => { }` as `showLaunchWindow` (`:32`).
- `src/CLIHub/Tray/TrayCommandHandlers.cs`: declares the dead `UpdateDownloadRequested` event (`:28`), raises it in `CreateCommands` (`:76`), takes `IApplicationLifetime` only to fill `Exit` (`:23,50,60,78`), and takes a `showLaunchWindow` parameter (`:67,77`) that the controller overwrites.

### Group 4: Tier-3 dead members

Evidence for each removal follows.

## Dead-code proof

| Item | Search | Result | Decision |
|------|--------|--------|----------|
| `SettingsPlaceholderPage.xaml` consumer | `rg "SettingsPlaceholderPage" --type xaml` | Only the host line at `SettingsWindow.xaml:477` and the placeholder's own `x:Class` | Remove: only consumed by the `IsPlaceholderPageVisible` binding, which is always false in production |
| `IsPlaceholderPageVisible` binding | `rg "IsPlaceholderPageVisible" src/CLIHub` | `SettingsWindow.xaml:477` (Visibility binding), `SettingsViewModel.cs:209,218,648` (property/notification) | Remove: only the host line binds it; production pages all pass `IsImplemented: true` |
| `IsImplemented` consumers | `rg "IsImplemented" src/CLIHub/ViewModels/Settings --type cs` | `SettingsNavigation.cs:14,33` (parameter/property), `SettingsViewModel.cs:218` (reads `SelectedPage.IsImplemented`) | Remove: only read is for `IsPlaceholderPageVisible`, and every production page passes `true` |
| `IsImplemented` value scan | `rg "IsImplemented.*true" src/CLIHub/ViewModels/Settings` | 9 entries in `SettingsNavigation.cs:58,65,72,80,89,97,104,111,118` | Confirmed: all production pages pass `true`; only tests construct `false` |
| `EmptyStateText` consumers | `rg "EmptyStateText" src/CLIHub` | `SettingsPlaceholderPage.xaml:9`, `SettingsStyles.xaml:154` (definition) | Remove style: only consumer is the placeholder page |
| `PageGroups` binding | `rg "PageGroups" src/CLIHub/Views/SettingsWindow.xaml` | No matches | Confirmed: XAML groups through `CollectionViewSource` over `Pages` at `:252-256,338` |
| `PageGroups` usage | `rg "PageGroups" src/CLIHub --type cs` | `SettingsViewModel.cs:187` (property), `SettingsNavigation.cs:124,137` (backing data) | Remove: no production bindings, only tests |
| `SettingsGroup` usage | `rg "SettingsGroup" src/CLIHub --type cs` | `SettingsNavigation.cs:44,124,126,137` (record def, PageGroupsData usage) | Remove if orphaned: only referenced by `PageGroups` |
| `ApplyStartupUpdateCheck` calls | `rg "ApplyStartupUpdateCheck" src/CLIHub tests/CLIHub.Tests` | `IPreferenceApplier.cs:30`, `PreferenceApplier.cs:84`, `SettingsApplicationService.cs:533`, 4 test mocks | Remove: implementation only logs, persisted state is the real read |
| `AgentProcess.Runtime` assignment | `rg "Runtime\s*=" src tests` | No matches targeting `AgentProcess` initializer (9 hits are all for `DefaultRuntime` preference) | Confirmed: never assigned |
| `AgentProcess.Runtime` read | `rg "\.Runtime\b" src tests` | No matches targeting `AgentProcess` property (11 hits are imports/preference paths) | Confirmed: never read in production or tests |

**Verdict**: all removals are dead; no live consumers found.

## Registration-home decision

**Decision (made by the orchestrator; recorded here)**: the app's current last-wins graph is the shipping graph, so preserve it exactly. Move the four DI-constructed registrations into `AddClIHubCoreServices` as one grouped `AddProcessServices` method in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`, delete the four duplicate registrations from `ServiceRegistration.cs` and from `WindowsServiceCollectionExtensions.cs` (Windows keeps `IInteractiveProcessRunner`, `IProcessOutputRunner`, `IRuntimeExecutableLocator`, `IAgentProcessInspector`, `IStartupService`, and the guard).

**Result**: the shipping graph is identical (same implementations, same injected dependencies, same singleton `AgentProcessMatcher` instance — no identity delta at all), and each of the four types has exactly one descriptor. Core may register the four because all four types are Core types; only the interfaces' implementations (`IRuntimeExecutableLocator`, runners, inspector) are Windows adapters.

**Alternative rejected**: move the Windows factories into Core. Rejected because the factories construct Windows-specific implementations and would force Core to reference Windows adapters, violating the layer boundary.

## Tray command shape decision

**Decision**: `TrayCommandHandlers.CreateCommands` will return a `TrayMenuCommands` record with inert default delegates for `RequestUpdateDownload`, `ShowLaunchWindow`, and `Exit` (e.g., `static () => { }`), and `TrayIconController.RefreshMenu` will replace them with the controller-owned implementations using `with` syntax. The `showLaunchWindow` parameter and `IApplicationLifetime` dependency are removed from `TrayCommandHandlers`; the controller supplies both slots directly.

**Rationale**: this preserves the established pattern where the controller owns the final behavior for these three slots, makes the ownership explicit, and removes the dead relay through `TrayActions`.

## Spec-text impact

This change removes three durable-spec statements but changes no runtime behavior:

1. **`openspec/specs/app-lifecycle/spec.md`** — Requirement "Single instance resource ownership", scenario "Second instance exits through the provider lifecycle" (lines 176-179): describes the defensive InstanceCoordinator check, which can only return `FirstInstance` in production because the guard is acquired in `ApplicationHost` before the provider is built.

2. **`openspec/specs/preferences-ui/spec.md`** — Requirement "Settings sections" (line 23): "A page whose settings are not yet implemented SHALL show a neutral empty state", and scenario "Page without implemented settings" (lines 52-55): describes the placeholder page fallback, which is unreachable because every production page passes `IsImplemented: true`.

3. **`openspec/specs/settings-theme/spec.md`** — scenario "Placeholder page readable" (lines 125-127): describes the placeholder page empty-state rendering, which is unreachable for the same reason.

These scenarios become descriptive-only after this change. They are not edited here (this is a behavior-preserving internal refactor marked `skip_specs: true`); reconciling them is parked in `docs/backlog.md` with a trigger: "the next change that touches Settings page metadata or startup instance handling, or a request to amend the lifecycle/preferences/settings-theme specs".

## Test plan

### Removed tests
- `tests/CLIHub.Tests/InstanceCoordinatorTests.cs` (entire file)
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:119-167` (three placeholder tests)
- ApplicationBootstrapper second-instance test (one test in `ApplicationBootstrapperTests.cs`)
- Any test in `TrayActionsTests.cs` that asserts the removed relay

### Added tests
1. **Descriptor uniqueness** (`ServiceRegistrationTests.cs`): `Assert.Single` for `IRuntimeResolver`, `ICommandLineBuilder`, `IAgentProcessFinder`, `IProcessOrchestrator` over the composed WPF service collection.
2. **Provider resolution** (`ServiceRegistrationTests.cs`): resolve each of the four and assert the expected concrete type (`RuntimeResolver`, `CommandLineBuilderAdapter`, `AgentProcessFinder`, `ProcessOrchestrator`).
3. **AgentProcessMatcher wiring** (`ServiceRegistrationTests.cs`): assert exactly one singleton `AgentProcessMatcher` descriptor, assert the `IAgentProcessFinder` descriptor's `ImplementationType` is `AgentProcessFinder` (so MS.DI injects the registered matcher instead of a factory calling `new AgentProcessMatcher()`), and `Assert.Same` on two matcher resolutions. No production seam or reflection.

### Strengthened tests
- `tests/CLIHub.Tests/Services/TrayActionsTests.cs`: fixture no longer passes `IApplicationLifetime`; replace any removed-relay assertion with one that tests the retained real path (the controller's `UpdateDownloadRequested` event).
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:71-72`: remove `IsImplemented` assertions.
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs:25`: update for removed `PageGroups`.
- `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs`: keep green; adjust its name/assertions only if it claims Windows registers the four.

### Coverage retained
- The ApplicationBootstrapper ordering test stays intact.
- All existing Settings page navigation tests remain.
- All existing tray tests remain except the removed relay test.
- The Windows adapter test still composes `AddClIHubCoreServices()` + `AddWindowsInfrastructure()`.

## Documentation updates

- `docs/architecture/startup.md`: remove stage 2 (InstanceCoordinator), renumber stages 3-7 to 2-6, remove the "Signaling the first instance defensively during bootstrapper startup" failure-policy row (line 72), remove the InstanceCoordinator bullet from the implementation list (line 55).
- `docs/architecture/ui.md`: remove step 2 mention of `IsImplemented` (line 76) from "Adding a Settings Page", and remove the placeholder-fallback smoke step (line 84-85).
- Check `docs/architecture.md` and `docs/project-context.md` for any other claim about the defensive second check or the placeholder page, and update if present.
- `docs/backlog.md`: add one entry recording the three now-descriptive-only spec scenarios from §Spec-text impact, with the reconciliation trigger. This is the only change to backlog.md and does not edit any spec.

## Risks

1. **AgentProcessMatcher identity**: the Windows factories construct `new AgentProcessMatcher()`, whereas the app's graph injects the `AgentProcessMatcher` singleton from Core. Moving to one registration home preserves the singleton identity in the app and the tests. **Mitigation**: the new identity test explicitly asserts the matcher singleton is the same instance the finder receives.

2. **DI ordering**: moving the four registrations into `AddClIHubCoreServices` means they are registered before `AddWindowsInfrastructure` instead of after. MS.DI descriptor order only matters when multiple descriptors exist for the same type; after this change, each type has exactly one descriptor, so ordering is irrelevant. **Mitigation**: the uniqueness tests explicitly assert one descriptor per type.

3. **IApplicationLifetime retention**: removing the `TrayCommandHandlers` dependency might suggest `IApplicationLifetime` is unused. **Verified**: `WindowActionCoordinator` still consumes it (`src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:21,43`). The registration MUST stay.

4. **Placeholder removal hides future extensibility**: if a new page needs the placeholder fallback, it must be re-added. **Accepted**: no page uses it today, and the removed code can be restored from history if needed. The registration-lives-in-two-places backlog item already covers adding pages.

5. **Spec-text drift**: the three removed scenarios remain in the spec files but describe unreachable paths. **Accepted**: this is a behavior-preserving internal refactor; reconciling the specs is parked with a clear trigger in `docs/backlog.md`.

## Open questions

1. **EmptyStateText style removal**: confirmed only the placeholder page uses it; remove the style definition in `SettingsStyles.xaml:154` together with the page. **Recommendation**: remove it; the search proves it's orphaned.

2. **SettingsGroup record removal**: if `PageGroups` is the only consumer, the `SettingsGroup` record becomes unreferenced. **Recommendation**: remove it if orphaned; keep it if any test or future extensibility path still references it.

3. **Windows adapter test name**: `WindowsServiceCollectionExtensionsTests` may claim Windows registers the four types. **Recommendation**: if the test asserts descriptor counts or specific registrations for the four, update its assertions to reflect that Core now registers them; if it only asserts that the composition succeeds, leave it unchanged.

4. **ApplicationBootstrapper ordering test**: the test may assert that `InstanceCoordinator.Coordinate()` runs before other startup stages. **Recommendation**: keep the test if it asserts a meaningful startup ordering constraint (e.g., `StartupStateLoader` before `ApplicationSession`); update it to remove the removed stage.

5. **TrayActions relay test**: no current test asserts the removed relay (`TrayActionsTests` covers state, launch, check-updates, and dispose only). The retained real path (`TrayIconController.UpdateDownloadRequested` → `WpfApplicationStartupUi` → `ShellCoordinator`) is already covered by `ShellCoordinatorTests.StartupUi_UpdateDownloadRequested_StartsDownloadWorkflow` and `ApplicationSessionTests`. **Decision**: update the fixture for the removed `IApplicationLifetime` argument, add a `TrayCommandHandlers.CreateCommands` test pinning the inert controller-owned slots, and do not construct `TrayIconController` in a test (it creates a real notify-icon and needs an STA host).

All recommendations are explicit; none require user confirmation.

## Implementation notes

- The implementer MUST run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` after each stage to confirm the tree stays green.
- Each stage ends with a buildable, testable intermediate state; no stage may leave half-edited call sites or broken references.
- UTF-8 safety: all edits via the edit tool, never PowerShell `Set-Content`/`Out-File`; UTF-8 without BOM; after each stage, run `git diff` and check for mojibake (`â€`, `Ã`, `ï»¿`, `U+FFFD`).
- Final counts: tests drop by the removed placeholder/instance/relay tests and rise by the new uniqueness tests. The implementer MUST report exact counts, not force a specific number.
- A mandatory user smoke task is included in `tasks.md`: start the app, exercise the tray menu, launch a second instance, open Settings and every page, launch an agent and run Detect/Check updates. Commit and archive wait for the user's confirmation.

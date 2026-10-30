## Verified Current State

### LaunchWindowViewModel.cs (src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs)

**Dead IUserNotificationService dependency:**
- Line 26: `private readonly IUserNotificationService notifications;` — field declared, never read beyond assignment.
- Line 49: XML doc parameter `<param name="notifications">` for the constructor.
- Line 61: Constructor parameter `IUserNotificationService notifications`.
- Line 71: Assignment `this.notifications = notifications;`.
- Repo-wide grep: `this.notifications` appears only at the assignment line in this file. `LaunchCommandCoordinator` at `src/CLIHub/ViewModels/Launch/LaunchCommandCoordinator.cs:36` owns the actual usage (line 105: `this.notifications.ShowInformation(version, $"{item.Name} version")`).

**WindowActionCoordinator inline construction:**
- Lines 78-87: `new WindowActionCoordinator(...)` with six services plus three VM delegates:
  - `launchCommandCoordinator`, `statusCoordinator`, `processMonitor`, `settingsLauncher`, `applicationLifetime`, `externalLauncher`
  - `() => this.SelectedAgent`, `this.ResolveLaunchContext`, `this.RefreshAgents`
- The coordinator is not registered in DI (`src/CLIHub/Composition/ServiceRegistration.cs:106-121` registers `LaunchWindowActionBuilder` and `LaunchWindowViewModel` as singletons but not `WindowActionCoordinator`).

**Constructor parameter count:**
- Lines 54-65: 12 parameters (including `IUserNotificationService notifications` at line 61).

### LaunchWindow.xaml.cs (src/CLIHub/Views/LaunchWindow.xaml.cs)

**Show/activate/hide policy:**
- Line 28: `private const long DeactivationGraceMilliseconds = 400;`
- Line 35: `private long shownAt;`
- Lines 65-99: `ShowOnPointerMonitor()`:
  - Line 67-70: Closed guard (`if (this.closed) return;`)
  - Line 72: `PopupScreenPosition` load from preferences
  - Line 74: `WindowPositioner.Place(this, position);`
  - Line 75: `this.viewModel.RefreshProjects();`
  - Lines 77-81: Comment + `this.viewModel.RefreshAgents();` + `this.viewModel.ApplyDefaultAgentSelection();`
  - Line 82: `this.Show();`
  - Lines 84-87: Minimized-state normalization
  - Line 89: `this.shownAt = Environment.TickCount64;`
  - Line 90: `this.Activate();`
  - Lines 92-98: Deferred placement via `Dispatcher.BeginInvoke(PlaceWhenOpen, ...)`
- Lines 120-126: `OnWindowActivated()`
  - Line 122: `this.viewModel.RefreshProjects();`
  - Lines 124-125: Comment + `this.viewModel.RefreshAgents();`
- Lines 128-141: `OnDeactivated()`
  - Lines 130-133: Pin/prompt guard (`if (this.viewModel.IsPinned || this.promptState.IsPromptOpen) return;`)
  - Lines 135-138: Grace period (`if (Environment.TickCount64 - this.shownAt < DeactivationGraceMilliseconds) return;`)
  - Line 140: `this.Hide();`

### ServiceRegistration.cs (src/CLIHub/Composition/ServiceRegistration.cs)

**Launch window DI registration (lines 106-121):**
- Line 108: `services.AddSingleton<IAgentCommandWorkflow, AgentCommandWorkflow>();`
- Line 112: `services.AddSingleton<LaunchCommandCoordinator>();`
- Line 117: `services.AddSingleton<LaunchWindowViewModel>();`
- `WindowActionCoordinator` is NOT registered.

Production DI resolves `LaunchWindowViewModel` by constructor, so removing the `IUserNotificationService` parameter automatically adjusts resolution from 12 to 11 parameters.

### LaunchWindowViewModelBuilder.cs (tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs)

**Build() method (lines 232-306):**
- Line 243: `var userNotificationService = this.userNotificationService ?? new Mock<IUserNotificationService>();`
- Line 274-278: `LaunchCommandCoordinator` construction with `userNotificationService.Object` at line 277.
- Lines 293-305: `LaunchWindowViewModel` construction with `userNotificationService.Object` at line 301.

**BuildWithMocks() method (lines 311-406):**
- Line 322: `var userNotificationService = this.userNotificationService ?? new Mock<IUserNotificationService>();`
- Line 353-357: `LaunchCommandCoordinator` construction with `userNotificationService.Object` at line 356.
- Lines 372-384: `LaunchWindowViewModel` construction with `userNotificationService.Object` at line 380.
- Lines 386-403: `TestMocks` record includes `UserNotificationService` at line 397.

**Duplication:** Lines 234-292 (Build) and 313-370 (BuildWithMocks) construct almost identical object graphs: project service, plugin catalog, agent services, logo cache, preferences, dialogs, workflows, lifetime, launchers, process inspector, project pane, agent pane, update control, status coordinator, launch coordinator, process monitor. The only difference is that `BuildWithMocks` returns `(viewModel, mocks)` while `Build` returns only `viewModel`.

**TestMocks (lines 462-479):**
- Line 473: `public required Mock<IUserNotificationService> UserNotificationService { get; init; }` — must remain because `LaunchCommandCoordinator` still needs it.

### MenuActionCoordinator

Repo-wide grep for `MenuActionCoordinator` returns 53 matches, all in archived changes under `openspec/changes/archive/`:
- `2026-10-09-extract-menu-action-coordinator/` — the change that introduced it.
- `2026-10-11-remove-init-agent-command/` — removed `InitCommand` from it.
- `2026-10-30-fix-launch-agent-filter/` — deleted it (task 1.1: "Delete `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs`").

No production or test references exist. The type is already gone.

### Filter synchronization

**Current implementation (LaunchWindowViewModel.cs):**
- Line 106: `this.actions.FilterAction.PropertyChanged += this.OnFilterActionChanged;` — subscription wired in constructor.
- Lines 308-315: `OnFilterActionChanged(object? sender, PropertyChangedEventArgs e)`
  - Line 310-314: If `IsChecked` changed and differs from `ShowOnlyProjectAgents`, update `ShowOnlyProjectAgents`.
- Lines 188-208: `ShowOnlyProjectAgents` property setter
  - Line 193: Field update with guard (`if (!this.SetProperty(ref field, value) || this.suppressFilterChange) return;`)
  - Lines 198-201: `PreferenceSyncHelper.SyncPreference` calls `preferences.ShowOnlyProjectAgents = value` and then `this.RefreshAgents`.
  - Lines 203-206: If `actions.FilterAction.IsChecked` differs, update it (loop prevention via `suppressFilterChange`).

**Bidirectional sync:** The property setter updates the menu action, and the menu action's property change updates the property. `suppressFilterChange` at line 36 prevents loops. This is the correct pre-regression wiring restored by the `fix-launch-agent-filter` change.

### Existing tests

**LaunchWindowViewModelFilterTests.cs** (actual test names, verified by reading the file):
- `ToggleFilterAction_UpdatesShowOnlyProjectAgents_PersistsPreference_RefreshesAgents` — takes the checkable entry from `viewModel.AgentsActions` (the same instance as `actions.FilterAction`, because `LaunchWindowActionBuilder.Build` returns the same `filterAction` in both places), sets `IsChecked = true`, and asserts `viewModel.ShowOnlyProjectAgents`, one `IPreferencesStore.Update`, and one detection call. This is the menu -> VM direction.
- `ToggleFilterAction_OnePersistenceCall_OneRefresh` — pins the no-loop / single-refresh guarantee for the same direction.
- `SetShowOnlyProjectAgents_MirrorsToFilterAction` — VM -> menu direction.
- `Constructor_SeedsShowOnlyProjectAgents_DoesNotPersist` — construction does not persist.

Both filter directions are already pinned, so this change must not add a duplicate filter test; it must keep these tests green.

## Design Decisions

### Decision 1: Remove dead IUserNotificationService dependency

**Chosen:** Delete the field, constructor parameter, and assignment from `LaunchWindowViewModel`. Keep `IUserNotificationService` in test builder's `TestMocks` because `LaunchCommandCoordinator` still needs it.

**Evidence:** Repo-wide grep shows `this.notifications` is assigned at line 71 and never read. `LaunchCommandCoordinator` owns the notification surface at line 105 of `LaunchCommandCoordinator.cs`.

**Impact:**
- `LaunchWindowViewModel` constructor drops from 12 to 11 parameters.
- Production DI resolves by constructor, so no registration edit needed.
- Test builder: remove `userNotificationService.Object` from lines 301 and 380 of `LaunchWindowViewModelBuilder.cs`.

### Decision 2: Move show/activate/hide policy into the view model

**Chosen:** Add four public methods to `LaunchWindowViewModel`:

1. **`PrepareForShow()`** — Calls `RefreshProjects()`, `RefreshAgents()`, `ApplyDefaultAgentSelection()` in that order. XML doc: "Prepares the window for display by refreshing both panes and applying the default agent selection. Version re-population is served by the version cache within its TTL. The default-agent lookup runs against the fresh agent rows."

2. **`PrepareForActivation()`** — Calls `RefreshProjects()`, `RefreshAgents()`. XML doc: "Refreshes both panes when the window is activated. Version re-population is cache-served within the TTL."

3. **`MarkShown()`** — Records `this.TickCountProvider()` in a private `long shownAt;` field (add after `suppressFilterChange`). XML doc: "Records the time the window was shown, resetting the deactivation grace period."

4. **`ShouldHideOnDeactivate(bool isPromptOpen)`** — Returns `false` if `IsPinned || isPromptOpen`; returns `false` if `this.TickCountProvider() - this.shownAt < DeactivationGraceMilliseconds`; otherwise returns `true`. XML doc: "Determines whether the window should hide on deactivation. Returns false if the window is pinned, a modal prompt is open, or the deactivation grace period has not elapsed."

Add `private const long DeactivationGraceMilliseconds = 400;` at line 38 (after the new `shownAt` field).

**Code-behind changes:**
- `ShowOnPointerMonitor`: Keep `WindowPositioner.Place`, `Show`, minimized-state normalization, `Activate`, closed guard, deferred `PlaceWhenOpen`. Replace `this.viewModel.RefreshProjects()` + `this.viewModel.RefreshAgents()` + `this.viewModel.ApplyDefaultAgentSelection()` (lines 75-81) with `this.viewModel.PrepareForShow()`. Replace `this.shownAt = Environment.TickCount64;` (line 89) with `this.viewModel.MarkShown()`. Remove the `shownAt` field (line 35) and `DeactivationGraceMilliseconds` constant (line 28) from code-behind.
- `OnWindowActivated`: Replace `this.viewModel.RefreshProjects()` + `this.viewModel.RefreshAgents()` (lines 122-125) with `this.viewModel.PrepareForActivation()`.
- `OnDeactivated`: Replace the pin/prompt/grace checks (lines 130-138) with `if (!this.viewModel.ShouldHideOnDeactivate(this.promptState.IsPromptOpen)) { return; }`.

**Rationale:** The project's MVVM guideline says "Keep WPF code-behind thin; put state, commands, and workflows in ViewModels or their existing collaborators." The show/activate/hide policy is a workflow, not WPF mechanics. Moving it to the VM makes it testable and keeps code-behind focused on `WindowPositioner.Place`, `Show`, `Activate`, `Hide`.

**Alternative rejected:** Keep policy in code-behind — violates MVVM guideline and leaves untestable logic.

**Test plan:** Add focused unit tests for:
- `PrepareForShow_CallsRefreshProjectsThenAgentsThenAppliesDefault` — verify order.
- `PrepareForActivation_CallsRefreshProjectsAndAgents` — verify both called.
- `ShouldHideOnDeactivate_PinnedReturnsFalse`.
- `ShouldHideOnDeactivate_PromptOpenReturnsFalse`.
- `ShouldHideOnDeactivate_WithinGraceReturnsFalse`.
- `ShouldHideOnDeactivate_AfterGraceReturnsTrue`.
- `MarkShown_ResetsGraceWindow`.

**Clock seam for grace tests:** Add an internal `Func<long> TickCountProvider { get; set; } = () => Environment.TickCount64;` property to the view model. `MarkShown` and `ShouldHideOnDeactivate` call `this.TickCountProvider()`. `CLIHub.csproj` already declares `<InternalsVisibleTo Include="CLIHub.Tests" />`, so the test builder can override the property after construction; the public 11-parameter constructor and production DI stay unchanged. This is a minimal seam with no new abstraction and justifies itself by enabling deterministic tests.

**Alternative rejected (constructor parameter):** A `Func<long>? getTickCount = null` constructor parameter would also work with MS.DI (verified: the container honors an optional null default), but it re-adds a twelfth parameter to the very constructor this slice is narrowing and widens the public surface for a test-only concern. The internal property is smaller.

**Alternative rejected:** Sleep 400 ms in tests — non-deterministic, slow, fragile.

### Decision 3: Keep inline WindowActionCoordinator construction

**Chosen:** Keep the inline `new WindowActionCoordinator(...)` construction at lines 78-87 of `LaunchWindowViewModel.cs`. Do not register it in DI and do not introduce a context type or factory.

**Reasoning:**
1. **Circularity:** `WindowActionCoordinator` needs three VM delegates (`getSelectedAgent`, `getCurrentProject`, `refreshAgents`). Naive DI creates a circular graph: VM -> coordinator -> VM. Resolving this requires either:
   - A factory: `Func<LaunchWindowViewModel, WindowActionCoordinator>` or a registered factory type.
   - A context type: `LaunchWindowActionContext` carrying the delegates, registered scoped/transient.
   - Inline construction (current).

2. **Cost/benefit:** The coordinator is constructed once per VM, with six singleton services and three lambda captures. A factory or context type adds abstraction for one call site. Inline construction is simpler.

3. **No reuse:** `WindowActionCoordinator` has one consumer (`LaunchWindowViewModel`). There is no DI-resolution scenario where another component would receive it.

**Alternative rejected:**
- `LaunchWindowActionContext` type: Adds a new type and registration for one call site. Deferred until a second consumer appears or the construction grows unwieldy.
- Factory: Same as context — adds abstraction for marginal benefit.

**Impact:** No DI registration changes. The inline construction stays at lines 78-87.

### Decision 4: Collapse LaunchWindowViewModelBuilder to shared Compose() helper

**Chosen:** Extract a private `Compose()` method that returns `(LaunchWindowViewModel viewModel, TestMocks mocks)`. Both `Build()` and `BuildWithMocks()` call it; `Build()` discards the mocks and returns only the VM.

**Reasoning:** Lines 234-292 (Build) and 313-370 (BuildWithMocks) duplicate 90% of the object-graph construction. The only difference is that `BuildWithMocks` returns the mocks. Extracting `Compose()` eliminates 60+ lines of duplication.

**Signatures stay unchanged:**
- `public LaunchWindowViewModel Build()` — calls `Compose()`, returns `viewModel`.
- `public (LaunchWindowViewModel ViewModel, TestMocks Mocks) BuildWithMocks()` — calls `Compose()`, returns `(viewModel, mocks)`.

Downstream tests compile unchanged because the builder's public API is preserved.

**Impact:** ~60 lines removed from `LaunchWindowViewModelBuilder.cs`.

### Decision 5: Do not add a filter test; keep the existing ones green

**Chosen:** Add no new filter test. The existing `LaunchWindowViewModelFilterTests` already pin both directions:
`ToggleFilterAction_UpdatesShowOnlyProjectAgents_PersistsPreference_RefreshesAgents` and
`ToggleFilterAction_OnePersistenceCall_OneRefresh` drive the checkable `AgentsActions` entry (the same
instance as `actions.FilterAction`) and assert the menu -> VM update, one persistence call, and one
refresh; `SetShowOnlyProjectAgents_MirrorsToFilterAction` covers VM -> menu.

**Rationale:** Re-adding the same coverage would only inflate the test count. The change's job is to
keep the constructor subscription at line 106 and the `OnFilterActionChanged` handler at lines 308-315
intact, and the existing tests fail if either is dropped.

## Test Plan

### New tests (new `tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelPolicyTests.cs`)

`LaunchWindowViewModelTests.cs` does not exist; create the new file (namespace `CLIHub.ViewModels`,
matching `LaunchWindowDefaultAgentTests`). `ProjectPaneController` and `AgentPaneController` are
sealed and cannot be mocked, so order is proven through the service mocks they call and through
observable VM state.

1. `PrepareForShow_RefreshesProjectsThenAgentsThenSelectsDefault` — callbacks on a shared order list:
   `IProjectService.GetAllProjects` ("projects"), `IAgentDetectionService.IsInstalledInSystem`
   ("agents"), `IPreferencesStore.Load` ("default"). Assert projects first, agents before default,
   and the configured default agent selected.
2. `PrepareForActivation_RefreshesProjectsAndAgentsWithoutDefaultSelection` — assert project and
   detection calls, `IPreferencesStore.Load` not called, and `SelectedAgent` unchanged (default
   selection must not run on activation).
3. `ShouldHideOnDeactivate_PinnedReturnsFalse` — tick 1000, pinned, `MarkShown()`, tick 2000, assert `false`.
4. `ShouldHideOnDeactivate_PromptOpenReturnsFalse` — tick 1000, `MarkShown()`, tick 2000, `isPromptOpen: true`, assert `false`.
5. `ShouldHideOnDeactivate_WithinGraceReturnsFalse` — tick 1000, `MarkShown()`, tick 1300, assert `false`.
6. `ShouldHideOnDeactivate_AfterGraceReturnsTrue` — tick 1000, `MarkShown()`, tick 1401, assert `true`.
7. `MarkShown_ResetsGraceWindow` — tick 1000, `MarkShown()`, tick 1300, `MarkShown()`, tick 1600, assert `false`.

### Existing test expectations

All existing tests must pass unchanged:
- `LaunchWindowViewModelBuilderTests` — tests the builder itself; signatures unchanged.
- `LaunchWindowDefaultAgentTests` — tests `ApplyDefaultAgentSelection`; still callable.
- `LaunchWindowViewModelFilterTests` — all four existing tests stay; both filter directions are
  already covered, so no filter test is added.
- `LaunchWindowViewModelDialogTests` — unaffected.
- `LaunchWindowClosedGuardTests` — tests code-behind closed guard; still callable.
- `WindowActionCoordinatorTests` — coordinator unchanged.
- `ServiceRegistrationTests` — no new registrations.

**Baseline:** 1,271 tests passing (544 CLIHub.Tests + 60 CLIHub.Core.Windows.Tests + 667 CLIHub.Core.Tests) before the change; 1,278 after the seven new tests.

## Open Questions

None blocking. Resolved during plan review:

1. **Tick seam naming and shape:** an internal `TickCountProvider` property (`Func<long>`), defaulting
   to `Environment.TickCount64`; no constructor parameter.
2. **Policy test file:** a new `LaunchWindowViewModelPolicyTests.cs`; the referenced
   `LaunchWindowViewModelTests.cs` does not exist.
3. **Grace constant location:** private in the view model; tests control elapsed time through the seam.
4. **Stage order:** the builder `Compose()` collapse lands before the policy tests, so the tick seam is
   wired once in `Compose()` instead of in both duplicated builders.

## Deviations from Brief

Corrected during plan review (see the resolved open questions):
1. The tick seam is an internal `TickCountProvider` property, not an optional constructor parameter
   (the suggested `() => Environment.TickCount64` default is not a legal C# parameter default, and a
   public parameter would re-widen the constructor this slice narrows).
2. Policy tests use service mocks and observable VM state; the pane controllers are sealed.
3. No filter test is added; the existing tests already pin both filter directions.
4. The builder `Compose()` collapse precedes the policy tests in the staged tasks.

## Files Modified

- `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs` — remove dead dependency, add policy methods and the internal tick seam.
- `src/CLIHub/Views/LaunchWindow.xaml.cs` — delegate to VM policy methods, remove `shownAt`/`DeactivationGraceMilliseconds`.
- `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` — remove `userNotificationService.Object` from two call sites, extract `Compose()`, add `WithTickCount`.
- `tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelPolicyTests.cs` (new) — add 7 policy tests.
- `tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelFilterTests.cs` — unchanged; existing tests pin both filter directions.

No DI registration changes (Decision 3).

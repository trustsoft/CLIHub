## Why

`LaunchWindowViewModel` carries a dead `IUserNotificationService` dependency, and the launch window's show/activate/hide policy is split between the view model and code-behind with no clear ownership boundary. Additionally, `WindowActionCoordinator` is constructed inline in the view model even though it depends on view-model delegates, creating an awkward construction sequence that could be cleaner with dependency injection.

This is slice 4 of the 2026-10-30 refactoring portfolio documented in `docs/backlog.md` (line 46).

**Dead dependency:** The `IUserNotificationService notifications` field at line 26 of `LaunchWindowViewModel.cs` is assigned at line 71 but never read anywhere else in the class. `LaunchCommandCoordinator` owns the actual notification surface. This adds a spurious constructor parameter and test-builder argument for no benefit.

**Show/activate/hide policy split:** The launch window's popup-shell behavior (show on pointer monitor, refresh on activation, hide on deactivate with grace period) is currently implemented in `LaunchWindow.xaml.cs` code-behind:
- `ShowOnPointerMonitor` (lines 65–99): closed guard, `PopupScreenPosition` load, `WindowPositioner.Place`, `RefreshProjects`, `RefreshAgents`, `ApplyDefaultAgentSelection`, `Show`, minimized-state normalization, `shownAt = Environment.TickCount64`, `Activate`, deferred placement.
- `OnWindowActivated` (lines 120–126): `RefreshProjects` + `RefreshAgents`.
- `OnDeactivated` (lines 128–141): pinned/prompt guard, 400 ms grace measured from `shownAt`, `Hide`.

This violates the project's MVVM guideline ("Keep WPF code-behind thin; put state, commands, and workflows in ViewModels or their existing collaborators"). The policy logic should live in the view model, leaving only pure WPF mechanics (placement, Show/Activate/Hide calls) in code-behind.

**Inline WindowActionCoordinator construction:** The view model constructs `WindowActionCoordinator` inline (lines 78–87) because the coordinator needs delegates back to the view model (`getSelectedAgent`, `getCurrentProject`, `refreshAgents`), creating a circular dependency that naive DI cannot resolve. This is workable but awkward.

## What Changes

### 1. Remove dead IUserNotificationService dependency

- Delete the `private readonly IUserNotificationService notifications;` field (line 26).
- Remove the `IUserNotificationService notifications` constructor parameter (line 61).
- Remove the `this.notifications = notifications;` assignment (line 71).
- Update production DI: `LaunchWindowViewModel` registration in `ServiceRegistration.AddLaunchWindowServices` resolves by constructor, so no registration edit is needed — the container will inject 11 parameters instead of 12.
- Update test builder: `LaunchWindowViewModelBuilder.Build()` at line 301 and `BuildWithMocks()` at line 380 pass `userNotificationService.Object` — remove that argument. Keep `userNotificationService` in `TestMocks` because `LaunchCommandCoordinator` still needs it.

### 2. Move show/activate/hide policy into the view model

Add four public methods to `LaunchWindowViewModel`:

- **`PrepareForShow()`** — Refreshes projects, agents, and applies default agent selection, in that order. Carries the existing comment that version re-population is cache-served within the TTL and the default-agent lookup must run against fresh rows.
  
- **`PrepareForActivation()`** — Refreshes projects and agents only (no default selection). Carries the cache-served version comment.

- **`MarkShown()`** — Records the current tick count in a private `long shownAt;` field. The tick source is an internal `Func<long>` seam defaulting to `Environment.TickCount64`, so the grace policy is testable without sleeping; the public constructor and DI registration are unchanged.

- **`ShouldHideOnDeactivate(bool isPromptOpen)`** — Returns `false` if `IsPinned` is true; returns `false` if `isPromptOpen` is true; returns `false` if fewer than 400 milliseconds have elapsed since `MarkShown()` was called; otherwise returns `true`.

The 400 ms constant moves into the view model as a private constant `DeactivationGraceMilliseconds`. The view model becomes the single owner of this policy.

**Code-behind delegation:** `ShowOnPointerMonitor` keeps `WindowPositioner.Place`, `Show`, minimized-state normalization, `Activate`, the closed guard, and the deferred `PlaceWhenOpen`; it calls `PrepareForShow()` and `MarkShown()` at the same points where the refresh calls and `shownAt` assignment happen today. `OnWindowActivated` calls `PrepareForActivation()`. `OnDeactivated` calls `ShouldHideOnDeactivate(this.promptState.IsPromptOpen)` and only hides if it returns `true`.

### 3. Evaluate WindowActionCoordinator injection

The brief's preference order is:
1. A small `LaunchWindowActionContext` type carrying the delegates supplied by the VM.
2. A factory, if genuinely clean.
3. Otherwise keep inline construction and record the decision with the circularity reasoning.

Do NOT force a circular dependency and do NOT add a coordinator abstraction with no consumer. `WindowActionCoordinator` remains the single owner of its commands, and the public command surface of the VM does not change.

### 4. Collapse LaunchWindowViewModelBuilder if mechanical

`Build()` (lines 232–306) and `BuildWithMocks()` (lines 311–406) construct almost identical object graphs. If they can be collapsed into one private `Compose()` helper without complicating the builder API, do so. The returned `TestMocks` shape must stay (all required properties, `UserNotificationService` included) and `Build()`/`BuildWithMocks()` must keep their exact signatures so downstream tests compile unchanged.

### 5. Prove MenuActionCoordinator is gone and filter sync works

Confirm `MenuActionCoordinator` is already deleted (repo-wide grep finds no type or reference). The filter sync lives in `LaunchWindowViewModel.OnFilterActionChanged` (lines 308–315) and `ShowOnlyProjectAgents` (lines 188–208): `actions.FilterAction.PropertyChanged` wired at line 106, bidirectional sync with `PreferenceSyncHelper.SyncPreference`. Existing tests already pin both directions (`ToggleFilterAction_UpdatesShowOnlyProjectAgents_PersistsPreference_RefreshesAgents`, `ToggleFilterAction_OnePersistenceCall_OneRefresh`, `SetShowOnlyProjectAgents_MirrorsToFilterAction`), so no new filter test is needed; they must stay green.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a behavior-preserving internal refactor. The `agent-availability-display` requirement "Agent pane refreshes when the launch window is shown or activated" is preserved exactly by `PrepareForShow`/`PrepareForActivation`, and the `app-lifecycle` popup-shell behavior is preserved by the code-behind policy delegation. The change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

**Modified code:**
- `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs` — remove dead `IUserNotificationService` dependency (field, ctor parameter, assignment); add `PrepareForShow()`, `PrepareForActivation()`, `MarkShown()`, `ShouldHideOnDeactivate(bool)` policy methods; decision on `WindowActionCoordinator` injection.
- `src/CLIHub/Views/LaunchWindow.xaml.cs` — delegate to VM policy methods in `ShowOnPointerMonitor`, `OnWindowActivated`, `OnDeactivated`; remove `shownAt` field and `DeactivationGraceMilliseconds` constant (moved to VM).
- `src/CLIHub/Composition/ServiceRegistration.cs` — potentially register `WindowActionCoordinator` if injection is chosen (decision in design).
- `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` — remove `userNotificationService.Object` from `Build()` line 301 and `BuildWithMocks()` line 380; potentially collapse to shared `Compose()` helper.
- `tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelPolicyTests.cs` — add focused unit tests for `PrepareForShow`, `PrepareForActivation`, `ShouldHideOnDeactivate`, and `MarkShown`; `LaunchWindowViewModelFilterTests` is unchanged.
- `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs` — remove the two `userNotificationService.Object` VM arguments; add `WithTickCount`.

**No behavior change:**
- Menu labels, filter semantics, persistence keys, window placement, DWM rounding, splitter drag, popup alignment, Escape handling, and `OnClosing` hide-instead-of-close stay exactly as they are.
- `WindowActionCoordinator`'s command semantics and process-monitor safety gates stay unchanged.
- Filter toggle loop guarantee from `agent-availability-display` spec stays intact.
- Public API changes minimized: four view-model methods added; no XAML binding changes; the public constructor shrinks from 12 to 11 parameters.

## Dependencies

Part of the 2026-10-30 refactoring portfolio (slice 4). Independent of other changes; does not block or depend on other planned work.

## Non-Goals

- No behavior change to menu labels, filter semantics, persistence keys, window placement, DWM rounding, splitter drag, popup alignment, Escape handling, or `OnClosing` hide-instead-of-close.
- No changes to `WindowActionCoordinator`'s command semantics or process-monitor safety gates.
- No fixes for unrelated backlog items (Settings defects, agent VM issues, dead placeholder pages, `IStartupWindow` marker).
- No new coordinator abstraction without a consumer.
- No weakening or deletion of tests.
- Do not touch unrelated docs working-tree changes.

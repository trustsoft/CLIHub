## Context

The launch window's "Only agents available in project" toggle renders and flips its checkbox but does not update the agent list. This is a regression introduced by commit `d6588f0`, which extracted `MenuActionCoordinator` from `LaunchWindowViewModel` to encapsulate action building and filter synchronization. The extraction dropped the subscription that connected the toggle to the view model.

## Goals / Non-Goals

**Goals:**
- Restore bidirectional synchronization between `FilterAction.IsChecked` and `LaunchWindowViewModel.ShowOnlyProjectAgents` with recursion guard
- Delete `MenuActionCoordinator` as dead code (no other production or test references)
- Add regression tests covering the toggle→VM→persistence→refresh flow, the VM→toggle mirror write-back, and the no-loop guarantee

**Non-Goals:**
- Changing menu labels, XAML binding, persistence key, or filter semantics
- Adding a new public API unless a test seam genuinely requires it
- Refactoring `LaunchWindowActionBuilder` or `LaunchWindowActions`

## Current State

### The Broken Wiring

**File:** `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs`

The coordinator listens to `FilterAction.PropertyChanged` at lines 88 and 94-100:

```csharp
this.filterAction.PropertyChanged += this.OnFilterActionChanged;

private void OnFilterActionChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(MenuAction.IsChecked))
    {
        this.OnPropertyChanged(nameof(this.ShowOnlyProjectAgents));
    }
}
```

It exposes a `ShowOnlyProjectAgents` property (lines 28-38) that reads from and writes to `filterAction.IsChecked`, and announces changes by calling `OnPropertyChanged`.

**Problem:** `LaunchWindowViewModel` constructs the coordinator (lines 96-106 in `LaunchWindowViewModel.cs`) but never subscribes to its property changes. The coordinator's `ShowOnlyProjectAgents` property announcements go nowhere.

**File:** `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs`

- Line 27: `private readonly MenuActionCoordinator menuCoordinator;`
- Lines 96-106: constructs `MenuActionCoordinator` with commands and the initial filter state from preferences
- Lines 112-114: seeds the VM's own `ShowOnlyProjectAgents` property from preferences with `suppressFilterChange = true`
- Lines 135 and 140: expose `ProjectsActions` and `AgentsActions` from `menuCoordinator`
- Lines 188-203: the VM's `ShowOnlyProjectAgents` setter persists via `PreferenceSyncHelper.SyncPreference` and calls `RefreshAgents()` when not suppressed

The VM's `ShowOnlyProjectAgents` property never writes its value into the filter action, and the VM never subscribes to the coordinator.

### Search Confirmation

Whole-solution search for `MenuActionCoordinator` returns exactly two production references:
1. `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs` — the class definition
2. `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:27` (field) and `:96-106` (construction)

No test file references `MenuActionCoordinator`.

### The Pre-Regression Model (commit `d6588f0^`)

```csharp
// Line numbers are from the commit before d6588f0
private MenuAction? _filterAction;

// Constructor
_filterAction = actions.FilterAction;
_filterAction.PropertyChanged += OnFilterActionChanged;

// Subscription handler
private void OnFilterActionChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(MenuAction.IsChecked)
        && _filterAction is { } action
        && action.IsChecked != ShowOnlyProjectAgents)
    {
        ShowOnlyProjectAgents = action.IsChecked;
    }
}

// VM property setter
public bool ShowOnlyProjectAgents
{
    get => _showOnlyProjectAgents;
    set
    {
        if (!SetProperty(ref _showOnlyProjectAgents, value) || _suppressFilterChange)
        {
            return;
        }

        _preferencesStore.Update(preferences => preferences.ShowOnlyProjectAgents = value);

        RefreshAgents();

        if (_filterAction is { } action && action.IsChecked != value)
        {
            action.IsChecked = value;
        }
    }
}

// Dispose
if (_filterAction is not null)
{
    _filterAction.PropertyChanged -= OnFilterActionChanged;
}
```

The bidirectional sync:
1. User toggles the filter action → `OnFilterActionChanged` assigns the VM property → persistence + refresh + mirror write-back to the action
2. Code sets the VM property → persistence + refresh + mirror write-back to the action

Recursion guard: `suppressFilterChange` prevents the constructor's initial assignment from persisting/refreshing/mirroring. The mirror write-back in the setter only fires after `SetProperty` returns true and the `suppressFilterChange` check passes, so the subscription handler's assignment does not loop.

### Spec Coverage

**File:** `openspec/specs/agent-availability-display/spec.md`

Requirement "Hide unavailable agents (optional filter)" (lines 65-96) covers the filter feature:
- Scenario "Filter enabled with a project selected" (lines 69-71): "WHEN the filter is enabled and a project is selected THEN only agents available in that project are listed"
- Scenario "Filter disabled" (lines 73-75): "WHEN the filter is disabled and a project is selected THEN all agents are listed, with unavailable ones dimmed"

The existing scenarios describe the filter's effect on the agent list but do not pin that the launch window's Actions-menu toggle is the trigger that applies the filter immediately.

## Decisions

### Decision 1: Restore the pre-regression ownership model rather than fixing MenuActionCoordinator

**Chosen:** Delete `MenuActionCoordinator` and restore the pre-regression wiring where `LaunchWindowViewModel` subscribes to `actions.FilterAction.PropertyChanged` and mirrors the value bidirectionally.

**Rationale:**
- `MenuActionCoordinator` has zero production or test references beyond the one field and construction site in `LaunchWindowViewModel`.
- Its sole purpose was to encapsulate the filter action subscription, but the encapsulation is unused — nothing subscribes to its `ShowOnlyProjectAgents` property.
- Restoring the pre-regression model is simpler than adding a bridge subscription (VM subscribes to coordinator, coordinator subscribes to filter action) and removes dead code.
- `LaunchWindowActionBuilder` already returns the `LaunchWindowActions` record, which contains the action lists and the filter action. The VM can own that record directly and expose `ProjectsActions`/`AgentsActions` without a wrapper.

**Alternatives considered:**
- Fix `MenuActionCoordinator` by having the VM subscribe to its `PropertyChanged`: Rejected — keeps dead abstraction and adds a second notification hop for no benefit.
- Keep `MenuActionCoordinator` and move all filter logic into it: Rejected — the VM already owns `ShowOnlyProjectAgents` as part of its public API and the persistence/refresh workflow; relocating that would be a larger refactor than the bug fix warrants.

### Relationship to the 2026-10-09 extraction decision

The archived change `2026-10-09-extract-menu-action-coordinator` introduced the coordinator and
explicitly listed "two-way preference synchronization" as a non-goal, assuming the view model
"can bind to `coordinator.ShowOnlyProjectAgents`". That assumption was never implemented: nothing
subscribed to the coordinator, so the filter stopped working at extraction time, not later. Its
stated goal of "zero functional changes" was therefore not met.

This change reverses that extraction because the coordinator's only value-add (encapsulating the
filter subscription) was never consumed, while it added a notification hop and an undisposed
subscription. The simpler ownership model - the view model owns the built `LaunchWindowActions`
and subscribes directly - restores the behavior and removes more code than it adds. If a future
menu workflow needs a real coordinator, it should be introduced with a consumer and tests, not as
a pass-through wrapper.

### Decision 2: Use the pre-regression recursion guard

**Chosen:** The `ShowOnlyProjectAgents` setter's mirror write-back and the subscription handler's conditional assignment together prevent a toggle loop.

**How it works:**
1. The constructor subscribes first, then seeds `ShowOnlyProjectAgents` with `suppressFilterChange = true`. The initial setter run skips persistence/refresh/mirror; no event can fire because `LaunchWindowActionBuilder` already pre-seeded `FilterAction.IsChecked` from the same preference value.
2. User toggles the filter action: `OnFilterActionChanged` checks that `action.IsChecked != ShowOnlyProjectAgents`, then assigns the VM property. The setter runs, persists, refreshes, and mirrors the value back to `action.IsChecked`. The mirror write-back sets `IsChecked` to the same value it already has, so `PropertyChanged` does not re-raise and the loop stops.
3. Code sets `ShowOnlyProjectAgents` directly: the setter persists, refreshes, and mirrors into `action.IsChecked`. If `IsChecked` changes, `PropertyChanged` raises but `OnFilterActionChanged` sees `action.IsChecked == ShowOnlyProjectAgents` (because the mirror write-back just synchronized them) and skips the assignment, stopping the loop.

**Rationale:** This was the pre-regression guard and it worked. No reason to invent a new one.

**Alternatives considered:** Add a `suppressMirror` flag in the subscription handler to prevent the mirror write-back — Rejected, overcomplicates the guard when the existing conditional assignments already break the loop.

### Decision 3: MODIFIED delta for agent-availability-display

**Chosen:** A MODIFIED delta that keeps every existing requirement and scenario name intact and adds one scenario under "Hide unavailable agents (optional filter)" pinning that the launch-window Actions-menu toggle applies the filter immediately.

**Rationale:**
- The existing requirement (lines 65-96) describes the filter's effect on the agent list but does not pin that the toggle is the UI trigger.
- The regression broke the toggle→VM connection; the new scenario ensures that connection is verified.
- The delta is one additional scenario under an existing requirement, not a new capability.

**Scenario to add:**

```markdown
#### Scenario: Launch window toggle applies filter immediately

- **WHEN** the user toggles "Only agents available in project" in the launch window's Agents-menu
- **THEN** the agent list is refreshed with the new filter value
- **AND** the preference is persisted
- **AND** toggling the filter does not cause a property-change loop
```

**Alternatives considered:**
- `skip_specs: true` — Rejected. The existing requirement covers the filter's effect but not the toggle wiring. The new scenario is narrow (one Actions-menu trigger) and belongs in the existing requirement as a behavioral pin.
- A NEW capability for "launch window filter toggle" — Rejected. This is not a new capability; it is a regression fix for specified behavior.

### Decision 4: Test seam via AgentsActions collection

**Chosen:** Tests will locate the filter action as `AgentsActions.Single(action => action.IsCheckable)`. No new public API will be added unless the test seam genuinely cannot reach the action through the existing collection.

**Rationale:**
- `AgentsActions` is already public (line 140 of `LaunchWindowViewModel.cs`).
- The filter action is the only checkable action in the Agents menu.
- Exposing the filter action as a separate public property would add API surface for test convenience only.

**Alternatives considered:** Add `FilterAction` as a public property on `LaunchWindowViewModel` — Rejected, test convenience does not justify new API.

## Risks

**Low-risk change:**
- This is a restoration of pre-regression behavior, not a new design.
- The bidirectional sync and recursion guard are identical to commit `d6588f0^`, which worked.
- `MenuActionCoordinator` has zero references beyond the one construction site; deleting it is safe.
- The test builder already constructs the full dependency graph; no builder changes are needed.

**Specific mitigations:**
- Build + test gates after each stage catch wiring errors immediately.
- The new tests verify the toggle→VM→persistence→refresh flow, the VM→toggle mirror, and the no-loop guarantee (one toggle = one persistence call + one refresh).

## Implementation Notes

**Task staging:**
1. Delete `MenuActionCoordinator` (the file, field, construction, and property delegation).
2. Restore the bidirectional sync in `LaunchWindowViewModel` (field, subscription, mirror write-back, unsubscribe in `Dispose`).
3. Add regression tests.
4. Gates: build, test, `openspec validate --all`.

**Changed files:**
- `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs` — deleted
- `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs` — subscription, mirror write-back, direct actions exposure, field/construction removal
- `tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelFilterTests.cs` (new test file) or extension of an existing test file

**No XAML, data, or public API changes.**

## Open Questions

None — the pre-regression model is documented in commit `d6588f0^`, the recursion guard is unchanged, and the test seam is through the existing `AgentsActions` collection.

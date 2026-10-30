## Why

The launch window's "Only agents available in project" toggle stopped working during a refactoring that introduced `MenuActionCoordinator`. The toggle state updates visually (the checkbox flips), but the view model never receives the change so the agent list is never filtered. `MenuActionCoordinator.OnFilterActionChanged` re-raises its own `ShowOnlyProjectAgents` property, but nothing subscribes to it — the view model constructs the coordinator but never subscribes to its property changes.

The fix is to restore the pre-regression ownership model where `LaunchWindowViewModel` owns the `LaunchWindowActions` instance directly and subscribes to `FilterAction.PropertyChanged`. The bidirectional sync that existed before commit `d6588f0` will be restored: the toggle updates the VM property (which persists and refreshes), and the VM property mirrors its value back to the toggle's `IsChecked` with recursion guard.

Since `MenuActionCoordinator` was only introduced to bridge this gap and has no other production or test references, it will be deleted as part of the fix.

## What Changes

1. **Restore the bidirectional filter sync:**
   - `LaunchWindowViewModel` will subscribe to `actions.FilterAction.PropertyChanged` (the `actions` instance built by the injected `LaunchWindowActionBuilder`).
   - When `FilterAction.IsChecked` changes and differs from `ShowOnlyProjectAgents`, assign the VM property (which persists via `PreferenceSyncHelper` and calls `RefreshAgents()`).
   - In the `ShowOnlyProjectAgents` setter, after the existing persistence/refresh work, mirror the value into `FilterAction.IsChecked` when not suppressed by the constructor's `suppressFilterChange` guard (which prevents recursion during initialization).
   - Unsubscribe in `Dispose`.

2. **Delete `MenuActionCoordinator`:**
   - Remove `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs` entirely.
   - Remove the `menuCoordinator` field from `LaunchWindowViewModel` and its construction.
   - Expose `ProjectsActions` and `AgentsActions` directly from the owned `LaunchWindowActions` instance.

3. **Add regression tests:**
   - Test that toggling the filter action updates `ShowOnlyProjectAgents`, persists the preference, and refreshes the agent pane.
   - Test that setting `ShowOnlyProjectAgents` updates `FilterAction.IsChecked` and does not persist/refresh during the constructor's suppression window.
   - Test that one user toggle causes one persistence call and one refresh (no loop).

## Capabilities

This change **restores the specified behavior** of the launch window's project-availability filter.

### Modified Capabilities

- **agent-availability-display** (behavior fix): The launch window's Actions-menu toggle now correctly applies the filter immediately by updating `LaunchWindowViewModel.ShowOnlyProjectAgents`, which persists the preference and refreshes the agent pane with the new filter value. Existing requirement "Hide unavailable agents (optional filter)" already covers this behavior; a new scenario will be added to pin that the toggle applies the filter immediately.

## Impact

**Modified code:**
- `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs` — subscription to `FilterAction.PropertyChanged`, mirror write-back in `ShowOnlyProjectAgents` setter, unsubscribe in `Dispose`, field/construction removal, direct actions exposure
- `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs` — deleted
- Tests — new regression coverage for the toggle wiring in `tests/CLIHub.Tests/ViewModels/Launch/` (extending the existing `LaunchWindowViewModelBuilder` if needed)

**No data migration, XAML, or UI label changes.**

## Non-Goals

- Changing the menu labels, XAML binding, persistence key, or filter semantics
- Changing `LaunchWindowActionBuilder` or the `LaunchWindowActions` record
- Adding a new public API (tests will locate the filter action via `AgentsActions.Single(action => action.IsCheckable)` if needed)
- Refactoring or renaming live code beyond what the fix requires

## Dependencies

None — standalone bug fix.

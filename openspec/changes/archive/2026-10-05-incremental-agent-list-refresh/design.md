## Context

See `proposal.md` for motivation. `LaunchWindowViewModel.RefreshProjects` already performs membership diff synchronization over stable project IDs. `RefreshAgents` currently composes a new list, clears `Agents`, and adds every item again. Agent version population now uses a generation and cancellation token, so the synchronizer must preserve the item identity expected by that lifecycle.

The existing `AgentItem` model raises property notifications. `AgentListComposer` already computes the target plugin entries and remains the source of availability policy.

## Goals / Non-Goals

**Goals:**

- Preserve `AgentItem` identity for unchanged plugin IDs.
- Apply additions, removals, and property changes with the smallest collection change set.
- Preserve selection by plugin ID.
- Keep `AgentListComposer` pure and unchanged as the availability decision point.
- Keep version population cancellation and generation checks correct.

**Non-Goals:**

- Changing detection rules or plugin ordering policy.
- Refactoring the retained legacy `MainWindow` into the new MVVM collection model.
- Adding collection virtualization or changing XAML templates.
- Removing all collection notifications; membership changes still notify the UI.

## Decisions

### 1. Synchronize by plugin ID

Build a target map keyed by `Plugin.Id`. For each target entry, update an existing `AgentItem` when the ID is present, otherwise create one. Remove current rows whose IDs are absent. Apply target order after membership synchronization so the UI order remains the composer order.

**Alternative rejected:** compare object references. Plugin instances and row instances can change independently across composition passes, while the plugin ID is the stable identity in the descriptor contract.

### 2. Keep the synchronizer outside Core

The synchronizer operates on `AgentItem` and `ObservableCollection`, so it belongs in the WPF project or ViewModels area. `AgentListComposer` remains Core policy code and returns target entries. The synchronization algorithm should be a small internal helper with unit tests that do not require a visible window.

### 3. Preserve selection explicitly

Capture the selected plugin ID before synchronization. After the target collection is applied, restore selection by ID if it remains. If it disappears, use the existing null-selection behavior.

### 4. Coordinate version population with synchronization

Start version population only after the target rows are synchronized, passing the current generation and cancellation token. Existing rows retain their current version while a new probe is pending; new rows begin with the existing unknown placeholder behavior.

## Risks / Trade-offs

- **[Risk] Reordering rows can create multiple move notifications.** → Keep the composer order and use a deterministic keyed synchronization helper; the list is small and membership churn is the primary target.
- **[Risk] A stale row property update reaches a reused item.** → Keep the existing generation/token guard and require the target plugin ID to match before applying asynchronous version results.
- **[Risk] Selection restoration triggers command requery or selection handlers.** → Suppress selection handling during synchronization, matching the existing project refresh pattern.

## Migration Plan

1. Add tests for keyed synchronization and selection preservation.
2. Implement the helper and switch `LaunchWindowViewModel.RefreshAgents` to use it.
3. Run agent availability, launch-window, and full solution tests.
4. Verify version population and refresh behavior manually in a Debug run.
5. Update the improvement backlog and archive the change after validation.

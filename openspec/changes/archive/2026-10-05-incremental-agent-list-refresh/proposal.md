## Why

The launch window currently rebuilds the entire agent collection whenever availability, project selection, filtering, or command completion changes. This creates unnecessary collection notifications, replaces row objects, and makes selection and version population work harder than necessary even though the agent list is keyed by stable plugin IDs.

The project list already uses identity-preserving diff synchronization. Applying the same boundary to agents will improve responsiveness and make refresh behavior predictable without changing which agents are shown.

## What Changes

- Synchronize the launch-window agent collection by stable plugin ID.
- Preserve existing `AgentItem` instances when an agent remains in the target list.
- Update availability, status, logo, and other row properties in place.
- Add and remove only membership changes.
- Preserve the selected agent when its plugin remains available.
- Keep version population generation/cancellation compatible with the existing runtime stabilization work.
- Add focused tests for additions, removals, updates, selection preservation, and ordering.
- Keep the retained legacy `MainWindow` path unchanged unless a small shared helper is needed for compilation or testability.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-availability-display`: Agent list refreshes SHALL preserve stable row identity and selection while applying availability changes.

## Impact

- Affects `LaunchWindowViewModel` agent collection synchronization and related view-model tests.
- May add a pure Core/UI-independent synchronization helper if that makes the diff behavior testable.
- Does not change plugin discovery, detection rules, version semantics, persistence, or the visible set of agents.
- Reduces UI collection churn and keeps current selection/version population stable across refreshes.

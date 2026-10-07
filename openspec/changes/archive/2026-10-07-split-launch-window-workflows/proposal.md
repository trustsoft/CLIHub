## Why

`LaunchWindowViewModel` currently owns project persistence and selection, agent composition and version refresh, command execution, update control, dialogs, notifications, and presentation state. This makes project and agent workflows difficult to test independently and increases the cost of changing either pane.

## What Changes

- Extract project-pane operations into a WPF-independent `ProjectPaneController`.
- Extract agent list, filtering, selection, refresh, cache invalidation, and version population into an `AgentPaneController`.
- Keep `LaunchWindowViewModel` as the binding-facing presentation facade for observable state, commands, status messages, and window actions.
- Preserve project selection identity, agent selection identity, asynchronous version population, application-operation lifetime tracking, and existing command results.
- Add focused controller tests and composition coverage for the new boundaries.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal refactor; `skip_specs: true` is set because externally observable behavior is preserved.

## Impact

- Affected `LaunchWindowViewModel`, project/agent pane workflow code, WPF composition registration, and application tests.
- No changes to persistence formats, plugin descriptors, agent command contracts, or user-facing behavior.

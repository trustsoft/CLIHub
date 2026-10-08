## Why

`LaunchWindowViewModel` still coordinates binding state with project and agent operations, command execution, menu construction, and an operating-system folder launch. Moving the remaining workflows behind focused boundaries will make command behavior independently testable while preserving the current launch-window experience.

## What Changes

- Formalize the existing project and agent pane controllers as the launch screen's pane boundaries.
- Extract agent command validation, execution, result mapping, and post-command refresh into a launch-command coordinator.
- Move pane Actions menu construction into a focused action builder.
- Introduce an application-facing external-launcher port for opening the CLIHub data folder.
- Reduce `LaunchWindowViewModel` to screen composition, binding state, and command forwarding.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor; user-visible behavior and existing lifecycle, agent-command, and launch-window requirements remain unchanged.

## Impact

Affected WPF application ViewModels, application ports, dependency registration, launch-window ViewModel tests, and architecture documentation. No Core contracts, preferences, UI interactions, or external dependencies change.

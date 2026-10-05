## Why

Agent launch from the tray and agent commands from the launch window currently reach `IAgentCommandService` through separate UI paths. A shared application workflow will centralize the project-context invocation while preserving each surface's status and error presentation.

## What Changes

- Add a shared agent command workflow that accepts a plugin, project, and command kind.
- Route launch-window commands and tray launch actions through the workflow.
- Preserve `IAgentCommandService` behavior and result semantics.
- Keep status updates in the launch ViewModel and warning dialogs in the tray UI.
- Add focused workflow tests and update DI composition.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal application-layer refactor; agent command requirements remain unchanged.

## Impact

- Affected runtime code: `LaunchWindowViewModel`, `TrayIconController`, WPF composition, and a new application workflow.
- Affected tests: focused workflow tests and full solution tests.
- Legacy `MainWindow` is out of scope and continues using its current path.
- OpenSpec specs are intentionally skipped because no observable requirement changes.

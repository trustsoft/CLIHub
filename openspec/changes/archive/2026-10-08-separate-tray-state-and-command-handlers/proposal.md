## Why

`TrayActions` currently projects tray state, subscribes to multiple state sources, and implements project, agent, update, settings, and release-notes handlers in one class. Separating projection from command handling will clarify ownership and make the tray workflows easier to test and reuse without changing menu behavior.

## What Changes

- Extract a tray-state projection service responsible for composing `TrayMenuState` and forwarding source changes.
- Extract tray command handlers for project, agent, update, settings, release-notes, and exit actions.
- Keep `ITrayActions`, `TrayMenuBuilder`, and `TrayIconController` compatibility boundaries stable while delegating through the focused services.
- Preserve current tray menu labels, update-check states, download requests, notifications, and disposal behavior.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor with no user-visible behavior change.

## Impact

Affected tray application services, composition registration, tray tests, and architecture documentation. Core contracts and WPF tray control ownership remain unchanged.

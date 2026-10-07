## Why

`TrayIconController` owns the WPF `TaskbarIcon`, menu construction, launch behavior, update notifications, and window visibility. This makes the host responsible for application rules and leaves tray behavior without focused tests.

## What Changes

- Keep `TrayIconController` focused on `TaskbarIcon` ownership, visibility, notification, refresh, and disposal.
- Move tray application actions and menu state into application-facing handlers.
- Make the menu builder consume prepared state and commands instead of reaching into workflows and WPF windows directly.
- Reuse the existing agent command workflow and launch-window/settings/release-notes actions.
- Add tests for menu state, action forwarding, refresh, and disposal.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal boundary refactor; `skip_specs: true` is set because observable tray behavior remains unchanged.

## Impact

- Affects `TrayIconController`, `TrayMenuBuilder`, tray action ports/handlers, WPF composition, and tray tests.
- No changes to menu labels, update behavior, agent launch workflow, or window lifecycle semantics.

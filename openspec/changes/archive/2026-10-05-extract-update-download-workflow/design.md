## Context

`App.DownloadAndApplyUpdateAsync` is the shared handler for tray and What's New download requests. It calls `IUpdateService.DownloadUpdateAsync`, reports downloaded or failed outcomes through the tray, refreshes the menu, waits two seconds so the notification is visible, then calls `ApplyDownloadedUpdateAndRestart`. The current method also catches unexpected failures and refreshes the menu.

## Goals / Non-Goals

**Goals:**

- Move the complete shared download/restart workflow out of `App.xaml.cs`.
- Keep tray and What's New requests routed to one coordinator singleton.
- Preserve notifications, logging, menu refresh, delay, and apply/restart order.
- Keep coordinator tests independent from WPF controls.

**Non-Goals:**

- Change `IUpdateService` or update result semantics.
- Change `UpdateControlViewModel` or manual launch-window update actions.
- Change notification text, delay duration, or update package behavior.
- Add confirmation dialogs, cancellation UI, or progress reporting.

## Decisions

- Add `IUpdateDownloadCoordinator` with `Task DownloadAndApplyAsync()`.
- Add `IUpdateDownloadNotifier` for downloaded/failed notifications and menu refresh; implement it with a WPF `UpdateDownloadNotifier` that dispatches to `TrayIconController`.
- Keep `UpdateDownloadCoordinator` dependent on `IUpdateService`, `IUpdateDownloadNotifier`, and `ILogger` only.
- Use an optional delay delegate in the coordinator constructor so tests can complete the downloaded path without waiting two seconds; production defaults to `Task.Delay`.
- Preserve the current result mapping: `Downloaded` notifies and applies after the delay; `Failed` logs, optionally notifies with its version, and refreshes; all other statuses log and refresh.
- Preserve the current catch boundary around download, delay, and apply; unexpected failures log and refresh without escaping into event handlers.

## Risks / Trade-offs

- **Tray and What's New could diverge again** -> Register one singleton coordinator and wire both events to it.
- **Notification could run off the UI thread** -> Put dispatcher marshalling in `UpdateDownloadNotifier`, not in the coordinator.
- **A test seam could affect production delay behavior** -> Keep the optional delegate defaulted to the real two-second `Task.Delay` and test the default-independent workflow path.

## Migration Plan

1. Add the coordinator, notifier interface, and WPF notifier implementation.
2. Register them in WPF composition.
3. Replace both `App` event handlers and remove the private download method.
4. Run focused tests, full build, and full test suite.
5. Rollback consists of restoring the private method and event handlers and removing the new registrations/types.

## Open Questions

None.

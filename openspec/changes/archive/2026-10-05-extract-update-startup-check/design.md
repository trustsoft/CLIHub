## Context

`App.OnStartup` starts `CheckForUpdatesAsync` only when `CheckForUpdatesOnStartup` is enabled. The current method calls `IUpdateService.CheckForUpdatesAsync`, notifies the tray when the result is `UpdateAvailable`, and catches/logs unexpected exceptions. The tray callback must remain marshalled through the WPF dispatcher.

## Goals / Non-Goals

**Goals:**

- Move startup update-check orchestration out of `App.xaml.cs`.
- Keep the check fire-and-forget from startup and preserve its internal exception boundary.
- Keep UI dispatch at the application boundary through an injected notification callback.
- Make enabled, disabled, available, non-available, and failure paths testable without WPF controls.

**Non-Goals:**

- Change `IUpdateService`, update result statuses, timeout behavior, or update state events.
- Change tray notification text or tray menu refresh behavior.
- Extract download/apply/restart; that is the next planned change.
- Change manual update checks in Settings, the launch window, or the legacy window.

## Decisions

- Add `IUpdateStartupCoordinator` with `Task CheckAsync(bool enabled, Action<string> notifyUpdateAvailable)`.
- The coordinator logs and returns immediately when startup checks are disabled.
- The coordinator invokes the callback only for `UpdateStatus.UpdateAvailable` with a non-null version.
- The coordinator catches unexpected exceptions and logs `Update check failed`, matching current behavior.
- `App.OnStartup` supplies a callback that uses `Dispatcher.Invoke` and `_tray?.NotifyUpdateAvailable`, preserving the existing WPF thread boundary.
- Register the coordinator as a singleton in WPF composition.

## Risks / Trade-offs

- **The callback could be invoked off the UI thread** -> Keep dispatcher invocation in the callback created by `App`, and test the coordinator with a simple recording action.
- **Download behavior could accidentally move with the check** -> Limit the coordinator to `CheckForUpdatesAsync` and available-version notification.
- **Disabled checks could still touch the update service** -> Add a disabled-path test that verifies no service call occurs.

## Migration Plan

1. Add the coordinator interface and implementation.
2. Register it in WPF composition.
3. Replace the inline startup check and remove the old private method.
4. Run focused tests, full build, and full test suite.
5. Rollback consists of restoring the inline method and removing the coordinator registration/types.

## Open Questions

None.

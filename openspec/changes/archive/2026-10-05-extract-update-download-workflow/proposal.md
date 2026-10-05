## Why

`App.OnStartup` currently owns the complete update download workflow, including result handling, tray notifications, menu refresh, restart delay, and apply/restart. Extracting it reduces startup class responsibilities and gives tray and What's New entry points one testable workflow boundary.

## What Changes

- Add an application-level update download coordinator.
- Move download result handling, notifications, menu refresh, delay, and apply/restart into the coordinator.
- Route both tray and What's New download requests through the same coordinator instance.
- Preserve current notification text, logging, status handling, and restart delay.
- Add a dispatcher-aware tray notification adapter and focused coordinator tests.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor; update-checking, update download, and release-notes-display requirements remain unchanged.

## Impact

- Affected runtime code: `App.xaml.cs`, WPF composition, tray notification boundary, and the new coordinator.
- Affected tests: focused download workflow and DI tests.
- `IUpdateService`, `UpdateDownloadResult`, and the existing `UpdateControlViewModel` workflow remain unchanged.
- OpenSpec specs are intentionally skipped because no observable requirement changes.

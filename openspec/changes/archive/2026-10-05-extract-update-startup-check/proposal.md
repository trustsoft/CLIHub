## Why

`App.OnStartup` currently owns the optional asynchronous update check, available-version decision, tray notification, and failure logging. Extracting this workflow creates a focused startup boundary while keeping update download/restart behavior separate for the next change.

## What Changes

- Add a startup update-check coordinator with an explicit enabled/disabled input.
- Preserve non-blocking startup behavior and warning-only failure handling.
- Preserve notification only when the update result reports an available version.
- Keep UI thread dispatch in the `App` callback that owns the tray interaction.
- Replace the inline startup update-check method in `App.OnStartup` and add focused tests.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor; update-checking and application lifecycle requirements remain unchanged.

## Impact

- Affected runtime code: `App.xaml.cs`, WPF application composition, and the new startup coordinator.
- Affected tests: focused enabled/disabled/result/failure tests and DI registration tests.
- Download, notification-after-download, apply, and restart behavior are out of scope.
- OpenSpec specs are intentionally skipped because no observable requirement changes.

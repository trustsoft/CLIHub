## Why

`LaunchWindowViewModel` and `TrayIconController` directly call `Application.Current.Shutdown()`, coupling application actions to the WPF singleton and making exit commands difficult to test. A narrow lifetime interface removes this static dependency while preserving the existing shutdown path.

## What Changes

- Add an application lifetime contract with a shutdown operation.
- Add a WPF implementation that delegates to the current application shutdown.
- Inject the contract into the launch window ViewModel and tray controller.
- Replace their direct static shutdown calls and add focused tests for the ViewModel command and DI registration.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal dependency-boundary refactor; application shutdown behavior remains unchanged.

## Impact

- Affected runtime code: `LaunchWindowViewModel`, `TrayIconController`, WPF composition, and a new lifetime adapter.
- Affected tests: focused exit-command and composition tests.
- No change to `App.OnExit`, resource disposal order, or application lifecycle behavior.
- OpenSpec specs are intentionally skipped because no observable requirement changes.

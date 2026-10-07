## Why

`PreferenceApplier` is application-facing code but currently depends directly on `GlobalHotkeyService` and `LaunchWindowViewModel`. This couples settings application to WPF construction and makes runtime, hotkey, startup, and display preference application difficult to test independently.

## What Changes

- Replace concrete hotkey and launch-window dependencies with narrow application-facing ports.
- Add a narrow runtime preference target over the process runtime boundary.
- Keep startup registration behind `IStartupService` and preserve existing hotkey re-registration rollback behavior.
- Keep display-style changes routed through the launch-window state port so normal property notifications continue.
- Add direct unit tests covering each preference branch and DI registration without constructing a WPF window.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal dependency-boundary refactor; `skip_specs: true` is set because observable Settings behavior is unchanged.

## Impact

- Affected `PreferenceApplier`, preference ports, runtime composition, `LaunchWindowViewModel`, and application tests.
- No changes to settings persistence, hotkey syntax, runtime selection, rollback semantics, or user-facing behavior.

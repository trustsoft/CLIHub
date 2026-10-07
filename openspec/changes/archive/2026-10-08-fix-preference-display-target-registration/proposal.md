## Why

The runtime preference decoupling introduced `IPathDisplayStyleTarget`, but the WPF composition root did not register the existing `LaunchWindowViewModel` as that target. Opening Settings therefore failed during dependency injection.

## What Changes

- Register `IPathDisplayStyleTarget` as the existing singleton `LaunchWindowViewModel`.
- Add a composition regression test covering the registration.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This restores the intended internal composition and does not change user-facing behavior.

## Impact

- Affects WPF DI registration and its application test coverage.
- Settings can again be opened after the preference application refactor.

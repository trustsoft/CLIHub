## Why

`App.OnStartup` currently applies runtime and Windows startup preferences inline with unrelated initialization steps. Extracting this small sequence gives startup orchestration a focused boundary while preserving the existing preference values, order, and failure behavior.

## What Changes

- Add one application startup service responsible for applying startup preferences.
- Preserve application of the configured default runtime and Windows startup registration.
- Replace the two inline calls in `App.OnStartup` with one injected service call.
- Keep `IPreferencesStore`, `IProcessLauncher`, and `IStartupService` contracts unchanged.
- Add focused tests for application order and DI registration.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor; existing startup and preferences requirements remain unchanged.

## Impact

- Affected runtime code: `App.xaml.cs`, WPF application composition, and the new startup preference service.
- Affected tests: focused orchestration and composition tests.
- No plugin, configuration, Core API, external dependency, or user-visible behavior changes.
- OpenSpec specs are intentionally skipped because no observable requirement changes.

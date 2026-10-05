## Why

`App.OnStartup` currently coordinates plugin seeding and plugin loading inline. Extracting this small sequence gives startup orchestration a clear responsibility boundary while preserving the established first-run behavior and keeping plugin services independently testable.

## What Changes

- Add one application startup service responsible for initializing the plugin catalog.
- Preserve the order: seed built-in plugins first, then load plugins.
- Replace the two inline `App.OnStartup` calls with one injected service call.
- Preserve the existing non-fatal seeding behavior and plugin loading behavior.
- Add focused tests for the orchestration order and DI registration.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is an internal refactor; the existing `plugin-seeding` and `app-lifecycle` requirements remain unchanged.

## Impact

- Affected runtime code: `App.xaml.cs`, WPF application composition, and the new startup service.
- Affected tests: composition and focused orchestration tests.
- No public Core API, plugin descriptor format, startup behavior, or external dependency changes.
- OpenSpec specs are intentionally skipped because no observable requirement changes.

## Why

`App.OnStartup` currently owns most application startup orchestration, including service resolution, plugin initialization, preferences, tray wiring, hotkey registration, release notes, and update checks. This makes startup ordering and failure policy difficult to test and leaves the WPF lifecycle adapter responsible for application concerns.

## What Changes

- Introduce an application bootstrapper boundary that owns first-instance startup orchestration after WPF has initialized.
- Move startup ordering, startup failure classification, and startup resource wiring out of `App.OnStartup`.
- Keep `App` responsible for WPF lifecycle events, dispatcher access, logging setup, and final process shutdown.
- Preserve the existing startup behavior, including second-instance activation, optional launch-window visibility, hotkey registration, release-notes evaluation, and best-effort update checking.
- Add direct tests for successful startup ordering and fatal versus best-effort startup failures.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `app-lifecycle`: Define the application startup orchestration boundary, ordering, and failure policy while preserving existing externally observable startup behavior.

## Impact

- Affected application composition and lifecycle code under `src/CLIHub`, especially `App`, service registration, and startup coordination services.
- New application-facing startup contract and test doubles in `tests/CLIHub.Tests`.
- No configuration format, plugin descriptor, update protocol, or user-facing startup behavior changes.

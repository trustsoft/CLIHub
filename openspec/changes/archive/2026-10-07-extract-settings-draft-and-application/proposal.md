## Why

`SettingsViewModel` currently owns string parsing, validation, draft state, persistence mutation, system preference application, and save failure handling. This makes Cancel/Save semantics difficult to test directly and allows a partially applied system preference to diverge from persisted configuration.

## What Changes

- Introduce a typed settings draft and an application service that loads, validates, saves, and applies settings.
- Move hotkey and numeric-field validation out of `SettingsViewModel` while preserving existing validation messages and defaults.
- Make Save persist only after required system preference application succeeds.
- Define rollback behavior when startup registration, hotkey registration, runtime/path application, or persistence fails.
- Keep `SettingsViewModel` responsible for WPF-bound properties, commands, update checks, and close/status presentation.
- Add direct application-service and ViewModel tests for Cancel, valid Save, invalid drafts, and partial-application failures.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `preferences-ui`: Settings save and validation behavior gains typed draft application and explicit rollback/error semantics.

## Impact

- Affected `SettingsViewModel`, settings application services, preference application contracts, and settings tests under `src/CLIHub` and `tests/CLIHub.Tests`.
- The external `config.json` format and existing Settings window binding surface remain unchanged.

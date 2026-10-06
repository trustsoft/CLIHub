## Why

Legacy configuration documents store the interactive runtime as `terminalExecutable`, while current settings use `defaultRuntime`. The migration runner now exists, so this compatibility transformation should run in the migration layer instead of remaining implicit in model defaults or runtime parsing.

## What Changes

- Register a schema 0 to schema 1 migration for the legacy terminal preference.
- Convert recognized legacy executable values such as `wt.exe`, `cmd.exe`, and PowerShell executables to the current runtime tokens.
- Fall back to Windows Terminal for missing or unrecognized legacy values, matching current defaults.
- Preserve unrelated preferences, project state, and the existing config document persistence behavior.
- Add tests for each supported legacy executable, missing/unknown values, idempotent current-schema loads, and migration failure handling.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `configuration-snapshot`: Add the legacy terminal preference migration behavior.

## Impact

- Affected Core migration registration, legacy preference compatibility, and configuration tests.
- Existing schema 0 documents become schema 1 in memory through the migration runner; ordinary explicit persistence writes the current schema metadata.
- Current schema 1 documents are not remigrated, and user project/preferences data is preserved.
- No new public settings UI or config file path is introduced.

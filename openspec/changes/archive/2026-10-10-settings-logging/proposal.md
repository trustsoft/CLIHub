## Why

The Telemetry & Logs page is a placeholder today, and logging is fixed: the level is read from `config.json` at startup but never exposed in the UI, and rotation is hardcoded to daily files with a 7-file retention and no size limit. Users cannot inspect the log location, copy its path, or open the folder.

## What Changes

- Expose the existing `logLevel` preference on the Telemetry & Logs page (Verbose, Debug, Information, Warning, Error); the value persists and takes effect on the next start.
- Add `LogRetentionDays` (default `7`) and `LogFileSizeLimitMb` (default `0` = unlimited); both are applied when the logger is created at startup.
- Show the current log file location with a Copy action, and offer an "Open log folder" action.
- These are additive preferences with defaults; no schema migration.
- No IPC named-pipe status and no anonymous-usage-diagnostics controls.

## Capabilities

### New Capabilities
<!-- Requirements land in the existing logging and preferences-ui capabilities. -->

### Modified Capabilities
- `logging`: configurable log retention and file size limit, and a log-location/open-folder surface.
- `preferences-ui`: the Telemetry & Logs page offers the log level, retention/size fields, log path with Copy, and Open log folder.
- `configuration-snapshot`: the new log preferences default safely when absent.

## Impact

- Core: `AppPreferences`, `AppConfigDocument`, `ConfigurationSnapshot.ClonePreferences`, `LoggingSetup.CreateLogger` (retention + size limit).
- App: `ApplicationHost.ConfigureLogging` (read the new preferences), `SettingsViewModel`/`SettingsApplicationService`, `SettingsTelemetryPage` (the former placeholder), `IExternalLauncher` (open folder), clipboard for Copy.
- Tests: logging setup tests, settings tests, serialization tests.

## Non-goals

- Applying the log level without a restart; IPC named-pipe status; anonymous usage diagnostics.

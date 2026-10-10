## Context

Logging is configured before the DI container starts: `ApplicationHost.ConfigureLogging` reads the level via `PreferenceReader.ReadLogLevel(config.json)` and calls `LoggingSetup.CreateLogger(logsDirectory, minimumLevel)`, which writes daily-rotated files into `AppPaths.Logs` with `retainedFileCountLimit: 7` and no size limit. `AppPreferences.LogLevel` exists but is not exposed in the UI. The Telemetry & Logs Settings page is a placeholder.

See `proposal.md` for motivation and the specs deltas for required behavior.

## Goals / Non-Goals

**Goals:**
- Expose the log level and add retention/size preferences, applied at the next start.
- Surface the log file location with Copy and an Open-folder action.
- Fill the Telemetry & Logs page.

**Non-Goals:**
- Runtime log-level switching; IPC/diagnostics controls.

## Decisions

### Additive preferences, read before DI
`AppPreferences` gains `LogRetentionDays = 7` and `LogFileSizeLimitMb = 0` (`0` = unlimited); `PreferencesDocument` mirrors them and `ConfigurationSnapshot.ClonePreferences` copies them. Because the logger is built before the container, `PreferenceReader` gains `ReadLogRetentionDays`/`ReadLogFileSizeLimitMb` (no services), and `ApplicationHost.ConfigureLogging` passes them to `LoggingSetup.CreateLogger`. No schema migration.

### Level and rotation apply on the next start
The logger keeps a fixed minimum level and fixed sink options; the Settings hint states that changes take effect after a restart. Alternative considered: a Serilog `LoggingLevelSwitch` for live level changes — rejected to avoid plumbing a switch through Core and the settings save path, since the agreed behavior is next-start.

### Size limit via the Serilog file sink
`LoggingSetup.CreateLogger` passes `retainedFileCountLimit` (retention) and `fileSizeLimitBytes` (size limit, `null` when the limit is `0`) with `rollOnFileSizeLimit` enabled when a limit is set.

### Log location in the view model
The current log file path is derived from `AppPaths.Logs` plus the current date (the sink's `clihub-YYYYMMDD.log` pattern). Copy uses the clipboard; Open log folder uses the existing `IExternalLauncher` to open the directory (falling back to the process shell if the launcher only opens files).

### Fill the placeholder page
The `telemetry` page becomes implemented (`SettingsPage.IsImplemented = true`); a new `SettingsTelemetryPage` `UserControl` is hosted by `SettingsWindow` and the placeholder view stops covering it.

## Risks / Trade-offs

- [Reading preferences cheaply before DI] → `PreferenceReader` already reads `config.json` directly; extend it the same way and treat malformed values as defaults.
- [Path derivation drift] → a single helper derives the current log path so the sink pattern and the UI stay in sync.
- [Open-folder on a non-existent directory] → ensure the logs directory exists before opening (the logger already creates it at startup).

## Migration Plan

No schema change. New preferences default when absent; the next write persists them.

## Open Questions

- Whether `IExternalLauncher` opens directories or a shell-open fallback is needed — settle during implementation; does not change the specs or task breakdown.

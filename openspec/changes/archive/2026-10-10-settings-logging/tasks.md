## 1. Preferences

- [x] 1.1 Add `LogRetentionDays` (default 7) and `LogFileSizeLimitMb` (default 0) to `AppPreferences`, mirror them in `PreferencesDocument`, and copy them in `ConfigurationSnapshot.ClonePreferences`; verify the serialization round-trip tests pass.

## 2. Logging pipeline

- [x] 2.1 Extend `PreferenceReader` to read the retention and size-limit preferences from `config.json` and pass them from `ApplicationHost.ConfigureLogging` into `LoggingSetup.CreateLogger`; verify the logging setup tests cover default and configured values.
- [x] 2.2 Apply retention via `retainedFileCountLimit` and the size limit via `fileSizeLimitBytes`/`rollOnFileSizeLimit` (null/off when zero); verify a logging test rolls over at the configured limit.

## 3. Settings end to end

- [x] 3.1 Add log level, retention-days, and file-size-limit fields to `SettingsInput`/`SettingsDraft` and persist them (validating retention > 0 and size >= 0); verify the settings application tests pass.
- [x] 3.2 Add the corresponding properties plus a log-file path, a Copy command, and an Open-log-folder command to `SettingsViewModel`; verify the view-model tests pass.
- [x] 3.3 Mark the `telemetry` page implemented and add `SettingsTelemetryPage` with the level selector, retention/size fields, log path with Copy, and Open log folder; verify the page is reachable and the placeholder no longer covers it.

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 4.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 4.3 Run `openspec validate settings-logging --strict` and verify the change is valid.
- [x] 4.4 Smoke-test: the Telemetry & Logs page shows the level, retention/size, and log path; Copy and Open log folder work; changing the level persists and applies after a restart.

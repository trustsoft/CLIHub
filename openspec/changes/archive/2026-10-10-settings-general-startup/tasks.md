## 1. Core model and persistence

- [x] 1.1 Add `StartMinimized` to `AppPreferences` (default `true`) and keep `ShowWindowOnStartup` as a documented legacy field; verify `dotnet build` succeeds.
- [x] 1.2 Copy `StartMinimized` (and the legacy field) in `ConfigurationSnapshot.ClonePreferences`; verify the configuration snapshot tests pass.
- [x] 1.3 Update `AppConfigDocument`: set `CurrentSchemaVersion = 2`, add `startMinimized` to `PreferencesDocument`, and write `showWindowOnStartup = !startMinimized` for backward compatibility; verify a round-trip test preserves the new field and JSON shape.

## 2. Schema migration

- [x] 2.1 Change `LegacyTerminalPreferenceMigration.TargetVersion` from `CurrentSchemaVersion` to a literal `1`; verify the migration-runner validation tests still pass.
- [x] 2.2 Add a schema 1→2 migration that sets `StartMinimized = !ShowWindowOnStartup` and register it in the Core composition; verify the app builds with both migrations registered.
- [x] 2.3 Add Core tests: a schema 1 document with `showWindowOnStartup` true maps to `startMinimized` false; false maps to true; unrelated preferences and projects are unchanged.

## 3. Startup behavior

- [x] 3.1 Update `ApplicationBootstrapper` to show the launch window unless `StartMinimized`; verify existing bootstrap tests pass and log text reflects start-minimized.

## 4. Settings end to end

- [x] 4.1 Rename the preference field in `SettingsDraft` and `SettingsInput` (`SettingsApplicationService`) to `StartMinimized`; verify the settings application tests pass.
- [x] 4.2 Rename `SettingsViewModel.ShowWindowOnStartup` to `StartMinimized`; verify the settings view-model tests pass.
- [x] 4.3 Update the General & Startup page toggle to "Start minimized to tray" bound to `StartMinimized`; verify the app builds and the label/binding are correct.

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 5.3 Run `openspec validate settings-general-startup --strict` and verify the change is valid.
- [x] 5.4 Smoke-test: with a schema 1 `config.json` (`showWindowOnStartup: false`) the app starts in the tray; toggling "Start minimized to tray" and saving changes the next start accordingly.

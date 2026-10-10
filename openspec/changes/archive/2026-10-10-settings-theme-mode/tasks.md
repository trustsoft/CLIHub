## 1. Preference model

- [x] 1.1 Add an `AppTheme` enum (`Dark`/`Light`/`System`) with tokens and a default-`Dark` parser; verify the parser tests cover known tokens, the default, and round-trip.
- [x] 1.2 Add `AppTheme` to `AppPreferences`, mirror it as a token in `PreferencesDocument`, and copy it in `ConfigurationSnapshot.ClonePreferences`; verify the serialization round-trip tests pass.

## 2. Theme resolution at startup

- [x] 2.1 Add a system-theme helper that reads the Windows `AppsUseLightTheme` setting (falling back to dark) and add `PreferenceReader.ReadAppTheme`; verify the helper tests cover light/dark/failure.
- [x] 2.2 Add `LightTheme.xaml` with the same brush keys as `LaunchTheme.xaml` and verify a key-parity check passes.
- [x] 2.3 Resolve the effective theme in `ApplicationHost` before windows load and append the light palette when light; verify the app builds and the default (dark) is unchanged.

## 3. Settings end to end

- [x] 3.1 Add the theme options and selected value to `SettingsViewModel` and persist it through `SettingsInput`/`SettingsDraft`; verify the settings tests pass.
- [x] 3.2 Add the "Application theme" selector and restart hint to `SettingsAppearancePage`; verify the page shows the three options with the stored value selected.

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 4.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 4.3 Run `openspec validate settings-theme-mode --strict` and verify the change is valid.
- [x] 4.4 Smoke-test: selecting Light and restarting renders all windows light; System follows the Windows personalization setting; Dark is the default and unchanged.

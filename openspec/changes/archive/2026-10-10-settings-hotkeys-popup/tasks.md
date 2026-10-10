## 1. Preference model

- [x] 1.1 Add a `PopupScreenPosition` enum with tokens and a default-`Center` parser; verify the parser tests cover known tokens and the default.
- [x] 1.2 Add `PopupScreenPosition` to `AppPreferences`, mirror it as a token in `PreferencesDocument`, and copy it in `ConfigurationSnapshot.ClonePreferences`; verify the serialization round-trip tests pass.

## 2. Window placement

- [x] 2.1 Generalize `WindowPositioner` to place by position (`Center`, `TopCenter`, `CursorRelative`, `PrimaryScreen`) keeping the native move, height cap, and layout fallback; verify the app builds.
- [x] 2.2 Make `LaunchWindow` read `PopupScreenPosition` from `IPreferencesStore` when shown and place accordingly; verify the window still hides-on-blur/pins as before.

## 3. Settings end to end

- [x] 3.1 Add the popup position options and selected value to `SettingsViewModel` and persist it through `SettingsInput`/`SettingsDraft`; verify the settings tests pass.
- [x] 3.2 Add the "Popup screen position" selector to `SettingsHotkeysPage`; verify the page shows the four options with the stored value selected.

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 4.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 4.3 Run `openspec validate settings-hotkeys-popup --strict` and verify the change is valid.
- [x] 4.4 Smoke-test: the Hotkeys & Launchers page shows the four positions; the launch window opens at the selected position on multi-monitor hardware; the default is unchanged; dismiss-on-blur and pinning still work.

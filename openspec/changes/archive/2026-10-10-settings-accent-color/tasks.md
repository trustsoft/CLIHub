## 1. Preference model

- [x] 1.1 Add an `AccentPalette` value type (presets, `#RRGGBB`/`#AARRGGBB` normalization, default Sky); verify the palette tests cover presets, normalization, and invalid input.
- [x] 1.2 Add `AccentColor` to `AppPreferences`, mirror it in `PreferencesDocument`, copy it in `ConfigurationSnapshot.ClonePreferences`, and add `PreferenceReader.ReadAccentColor`; verify the serialization tests pass.

## 2. Live application

- [x] 2.1 Add `IAccentPreferenceTarget` and a WPF `WpfAccentPreferenceTarget` (in `Views/`) that overrides `AccentBrush`/`AccentSelectionBrush`; register it and add `PreferenceApplier.ApplyAccent`; verify the app builds and the architecture boundary test passes.
- [x] 2.2 Apply the accent in `SettingsApplicationService.Save` before persistence with rollback on failure; verify a settings test covers apply and rollback.

## 3. Startup application

- [x] 3.1 Apply the stored accent in `App.xaml.cs` before windows render; verify a stored accent is applied on start.

## 4. Settings end to end

- [x] 4.1 Add preset options, a selected accent, and a custom hex field to `SettingsViewModel`, and persist through `SettingsInput`/`SettingsDraft` (reject invalid custom input); verify the settings tests pass.
- [x] 4.2 Add the accent swatches and custom field to `SettingsAppearancePage`; verify the page shows the presets and applies the stored value.

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 5.3 Run `openspec validate settings-accent-color --strict` and verify the change is valid.
- [x] 5.4 Smoke-test: selecting a preset or entering a custom hex changes the accent immediately; saving persists it; restarting keeps it; an invalid hex is rejected.

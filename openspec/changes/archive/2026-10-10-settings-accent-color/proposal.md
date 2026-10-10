## Why

The accent is a fixed sky blue. Users want to choose the accent used across the app. Because `AccentBrush` is now a `DynamicResource`, the accent can change live without a restart.

## What Changes

- Add an `AccentColor` preference with the presets **Sky** (default `#4CC2FF`), **Windows Blue** `#3B82F6`, **Emerald** `#10B981`, **Purple** `#8B5CF6`, and **Amber** `#F59E0B`, plus a custom `#RRGGBB` entry.
- Apply the accent **live**: saving a new accent overrides `AccentBrush` (and the accent selection tint) in the application resources immediately; the value also applies on later starts.
- An invalid custom value is rejected; a missing or invalid stored value falls back to the Sky default.
- The Appearance page gains the accent selector and custom field.

## Capabilities

### New Capabilities
<!-- None; the accent is part of the application theme. -->

### Modified Capabilities
- `application-theme`: add the accent color preference with live application.
- `preferences-ui`: the Appearance page offers the accent selector and custom field.
- `configuration-snapshot`: the accent color defaults safely for missing or invalid values.

## Impact

- Core: `AppPreferences`, `AppConfigDocument`, `ConfigurationSnapshot.ClonePreferences`, an `AccentPalette` value type (presets, normalization, validation), `PreferenceReader`.
- App: `App.xaml.cs` (apply the stored accent at startup), a WPF accent adapter (`IAccentPreferenceTarget`), `PreferenceApplier`/`SettingsApplicationService` (apply on save), `SettingsViewModel`, `SettingsAppearancePage`.
- Tests: accent palette tests; settings tests; serialization tests.

## Non-goals

- Per-window accents; accent-derived palette recolouring beyond the accent and its selection tint; window opacity/blur.

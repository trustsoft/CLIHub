## Why

The application is dark-only. Users on light-preferring systems cannot lighten the UI or follow the Windows personalization setting. This adds an application theme preference.

## What Changes

- Add an `AppTheme` preference with the values **Dark** (default), **Light**, and **System**, stored as a token.
- Introduce a Light palette dictionary that defines the same brush keys as the dark palette; at startup the application merges the Dark or Light palette according to the preference.
- When the preference is **System**, resolve the effective theme from the Windows personalization setting (apps-use-light-theme).
- The Appearance page gains an "Application theme" selector.
- The theme is applied on the next start (the palette is chosen before any window loads); there is no live switching.
- No accent color, window opacity/blur, or compact density in this change.

## Capabilities

### New Capabilities
- `application-theme`: application-wide Light/Dark/System theme selection and startup palette resolution.

### Modified Capabilities
- `preferences-ui`: the Appearance page offers the application theme selector.
- `configuration-snapshot`: the theme preference defaults safely for missing or unknown values.

## Impact

- Core: `AppPreferences`, `AppConfigDocument`, `ConfigurationSnapshot.ClonePreferences`, an `AppTheme` value type + parser, `PreferenceReader` (theme token), and a Windows-theme resolver.
- App: `App.xaml` replaces the fixed dark palette merge with a startup-selected palette; a new `LightTheme.xaml`; `ApplicationHost` resolves the effective theme before windows load; `SettingsViewModel`/`SettingsApplicationService`; `SettingsAppearancePage`.
- Tests: theme parsing tests; settings tests; light-palette key-parity test.

## Non-goals

- Accent color; window opacity and blur; compact density; live theme switching.

## Why

The Appearance page now presents the accent color as preview cards, the preset palette has grown to
cover more hues, and the custom entry remembers its last value independently of the named presets.
The durable specs still describe the old segmented selector and the original five presets.

## What Changes

- Expand the accent presets with Teal, Indigo, and Tangerine, and rename "Windows Blue" to "Blue".
- Present the accent choice as preview cards with a color dot, name, and hex value, an active-accent
  badge, and a selected ring/check.
- Show the custom hex field only while the custom entry is selected.
- Remember the last custom accent independently of the selected preset for the lifetime of the
  running application, including when the Settings window is closed and reopened.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `application-theme`: The accent color preference lists the refreshed preset set and the Blue rename.
- `preferences-ui`: The accent color selector requirement describes the preview-card picker, the
  conditional custom field, and the remembered custom value.

## Impact

- `src/CLIHub.Core/Models/Appearance/AccentPalette.cs` - preset names and colors.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` - accent choices and custom-value memory.
- `src/CLIHub/ViewModels/Settings/AccentSelectionMemory.cs` - process-wide last custom accent.
- `src/CLIHub/Composition/ServiceRegistration.cs` - singleton registration.
- `src/CLIHub/Views/SettingsAppearancePage.xaml`, `src/CLIHub/Themes/SettingsStyles.xaml` - picker UI.
- Tests covering the accent selection and palette.

## Why

The Appearance page presents the application theme as a compact segmented control, so the three
choices read as plain labels: the user cannot see what each theme looks like before committing to it.
The attached Settings target shows the theme choice as three preview cards, each with a miniature
palette sample, a descriptive label, and an explicit selected state.

## What Changes

- Replace the segmented theme selector with three preview cards laid out in one row.
- Label the options "Dark Theme", "Light Theme", and "System Sync" and add a one-line description to
  each: "Windows 11 Fluent Dark (Recommended)", "High contrast daylight profile", and
  "Follow Windows personalization state".
- Give each card a miniature palette preview (dark surface, light surface, and a dark-to-light split
  for the Windows-synced option).
- Mark the selected card with an accent ring and an accent check badge.
- Keep the existing restart hint, folded into the setting hint together with the Windows-follow note.
- Keep the selecting, loading, saving, and persistence behavior unchanged; the picker still offers
  exactly the three `AppTheme` values and stores the same tokens.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: The "Application theme selector" requirement now describes a row of preview
  cards with the new option labels and descriptions, the accent ring, and the check badge.

## Impact

- `src/CLIHub/Views/SettingsAppearancePage.xaml` - the theme setting becomes a preview-card list.
- `src/CLIHub/Themes/SettingsStyles.xaml` - new `ThemeOptionGroup`/`ThemeOptionItem` styles and the
  fixed preview palette used by the cards.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` - `AppThemeOption` gains a description and the
  three option labels change.
- `openspec/specs/preferences-ui/spec.md` - updated through the change archive.

# Proposal: Theme & Accent Copy Revision

## Why

The Settings → Theme & Accent page copy includes verbose section headers, hints, and labels that can be made more concise while retaining clarity. The user reviewed a mockup (`mockup-settings-theme-accent.html`) that removes the section header, shortens the hints, and streamlines the theme names and descriptions. The user approved the "main variant" copy for implementation.

This change applies only approved copy edits. No behavior, layout beyond vertical rhythm, style tokens, control ordering, or settings contract changes are included.

## What Changes

### XAML (`src/CLIHub/Views/SettingsAppearancePage.xaml`)

1. Remove the `<controls:SectionHeader Text="THEME"/>` element (line 10).
2. Set the first `SettingCard` (line 11) to `Margin="0,18,0,8"` to start 18 px below the divider, mirroring the archived `2026-11-03-logs-page-copy` vertical rhythm pattern. The second card keeps the shared `0,0,0,8` card margin.
3. Theme card title (line 11): `Application theme` → `Theme`.
4. Theme card hint (line 12): `Select the visual palette and luminosity mode. System Theme follows Windows.` → `System follows the Windows light/dark mode.`
5. Accent card hint (line 72): `Applied to active states, focus rings, toggles, and primary actions.` → `Used for selection, focus, and emphasis.`
6. Custom accent TextBox tooltip (line 158): `Custom accent color as #RRGGBB` → `Hex color, e.g. #4CC2FF`.

### Theme Descriptor Data (`src/CLIHub/Themes/ThemeDescriptors.cs`)

7. Theme names (lines 48, 55, 62, 69): `Dark Theme` → `Dark`, `Light Theme` → `Light`, `Deep Blue Theme` → `Deep Blue`, `System Theme` → `System`.
8. Theme descriptions (lines 49, 56, 63, 70): `Fluent Dark (default)` → `Default palette`, `High contrast daylight profile` → `Bright, high-contrast palette`, `Blue-tinted dark chrome` → `Dark with a blue tint`, `Follow Windows personalization state` → `Follows the Windows light/dark mode`.

### Accessible Names for Theme Options (`src/CLIHub/ViewModels/Settings/SettingsOptions.cs`)

9. Add an `AccessibleName` property to `AppThemeOption` that produces self-describing screen-reader names: `Dark theme`, `Light theme`, `Deep Blue theme`, `System theme` (the visible names become short, but accessible names remain self-describing).
10. Update the `AutomationProperties.Name` binding in `src/CLIHub/Themes/ThemePickerData.xaml` (line 38) from `{Binding Name}` to `{Binding AccessibleName}`.

### Accent Custom Entry (`src/CLIHub/ViewModels/Settings/AccentSelection.cs`)

11. Custom entry name (line 12): `Custom...` → `Custom`.
12. Custom entry hint (line 12): `RGB / Hex` → `Your own color`.

### Accessible Name for Custom Accent Entry (`src/CLIHub/ViewModels/Settings/SettingsOptions.cs`)

13. Update `AccentChoice.AccessibleName` computation (line 82) to produce `Custom accent color` for the custom entry (avoiding the awkward `Custom Your own color`) while presets keep `Name + " " + Hint` (e.g. `Sky #4CC2FF`).

### Settings Page Description (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`)

14. Page description (line 93): `Theme and accent color.` → `How CLIHub looks.`

### Explicit Non-Changes

- `Accent color` card title is unchanged.
- Badge `Active: Sky` / `Active: Custom` and `ActiveAccentLabel` behavior are unchanged.
- Accent preset names and visible hex values (`#4CC2FF`, etc.) are unchanged.
- `Custom color` field label and `#RRGGBB` placeholder are unchanged.
- Validation message `Accent color must be a hex value like #RRGGBB.` is unchanged.
- `Save` / `Cancel`, ordering, icons, colors, spacing beyond the first-card top margin, and all behavior are unchanged.
- The optional compact-hex variant (hiding hex values) is NOT implemented.

## Impact

- User-facing copy is more concise.
- Vertical rhythm is preserved after removing the section header by setting the first card's top margin to 18 px.
- Accessible names for theme options and the custom accent entry remain self-describing for screen readers.
- The `preferences-ui` spec requires a MODIFIED requirement for `Application theme selector` to reflect the new theme names and descriptions. The accent selector requirement does not change because hex values remain visible and the custom hint is not pinned by the spec.
- No README screenshot update is included in this change (the existing screenshot is already stale from prior changes; release-preparation owns screenshot refresh).

## Dependencies

None. This change is independent and has no blocking or dependent changes.

# Design: Theme & Accent Copy Revision

## Current State (HEAD 3fac3bd)

### XAML Structure (`src/CLIHub/Views/SettingsAppearancePage.xaml`)

```xaml
10:         <controls:SectionHeader Text="THEME"/>
11:         <controls:SettingCard Title="Application theme"
12:                            Hint="Select the visual palette and luminosity mode. System Theme follows Windows."
...
71:         <controls:SettingCard Title="Accent color"
72:                            Hint="Applied to active states, focus rings, toggles, and primary actions."
...
158:                              ToolTip="Custom accent color as #RRGGBB"/>
```

### Theme Descriptors (`src/CLIHub/Themes/ThemeDescriptors.cs`)

```csharp
46:         new ThemeDescriptor(
47:             AppTheme.Dark,
48:             "Dark Theme",
49:             "Fluent Dark (default)",
...
54:         new ThemeDescriptor(
55:             AppTheme.Light,
56:             "Light Theme",
57:             "High contrast daylight profile",
...
61:         new ThemeDescriptor(
62:             AppTheme.Blue,
63:             "Deep Blue Theme",
64:             "Blue-tinted dark chrome",
...
68:         new ThemeDescriptor(
69:             AppTheme.System,
70:             "System Theme",
71:             "Follow Windows personalization state",
```

### AppThemeOption Record (`src/CLIHub/ViewModels/Settings/SettingsOptions.cs`)

```csharp
62: public sealed record AppThemeOption(
63:     AppTheme Theme,
64:     string Name,
65:     string Description,
66:     string PreviewCardBrush,
67:     string PreviewBarBrush,
68:     bool UsesAccentPreviewDot);
```

The record has no `AccessibleName` property. The binding in `ThemePickerData.xaml:38` uses `{Binding Name}` for `AutomationProperties.Name`.

### AccentChoice Record (`src/CLIHub/ViewModels/Settings/SettingsOptions.cs`)

```csharp
77: public sealed record AccentChoice(string Name, string Hex, string Hint, bool IsCustom)
78: {
79:     /// <summary>
80:     ///   The accessible name combining the label with the color value or hint.
81:     /// </summary>
82:     public string AccessibleName => $"{this.Name} {this.Hint}";
83: }
```

The current formula produces `Custom... RGB / Hex` for the custom entry and `Sky #4CC2FF` for presets. After the approved copy changes, it would produce `Custom Your own color` — awkward but not incorrect.

### Accent Custom Entry (`src/CLIHub/ViewModels/Settings/AccentSelection.cs`)

```csharp
11:     private static readonly AccentChoice CustomAccentChoice =
12:         new("Custom...", "Transparent", "RGB / Hex", true);
```

### Settings Navigation (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`)

```csharp
91:             "appearance",
92:             "Theme & Accent",
93:             "Theme and accent color.",
```

## Design Decisions

### 1. Vertical Rhythm After Removing Section Header

The `SectionHeader` style applies `Margin="0,18,0,8"` (18 px top, 8 px bottom). Removing it requires moving the top margin to the first `SettingCard`. Set `Margin="0,18,0,8"` on the theme card (line 11) so it starts 18 px below the page divider and leaves 8 px below itself. The accent card keeps the default `Margin="0,0,0,8"`.

This mirrors the archived `2026-11-03-logs-page-copy` pattern (`proposal.md:40-45`), which also removed section headers and moved the 18 px top margin to the first group.

### 2. Accessible Names for Theme Options

The short visible names (`Dark`, `Light`, `Deep Blue`, `System`) lose the word "theme" that clarifies what they describe. Screen readers need the full context.

**Decision:** Add an `AccessibleName` computed property to `AppThemeOption` that appends " theme" to `Name`:

```csharp
/// <summary>
///   The accessible name combining the theme name with "theme" for screen readers.
/// </summary>
public string AccessibleName => $"{this.Name} theme";
```

This produces `Dark theme`, `Light theme`, `Deep Blue theme`, `System theme` while keeping the visible names short. Update the binding in `ThemePickerData.xaml:38` from `{Binding Name}` to `{Binding AccessibleName}`.

**Alternative rejected:** A separate field in the record would require updating every `AppThemeOption` construction site. A computed property is simpler and keeps the accessible name synchronized with the visible name.

### 3. Accessible Name for Custom Accent Entry

The current formula `Name + " " + Hint` would produce `Custom Your own color` with the approved copy. This is awkward but technically describes the control.

**Decision:** Special-case the custom entry to produce `Custom accent color` while presets keep `Name + " " + Hint`. Modify the `AccessibleName` property:

```csharp
public string AccessibleName => this.IsCustom
    ? "Custom accent color"
    : $"{this.Name} {this.Hint}";
```

This makes the custom entry's accessible name clearer without changing preset behavior.

**Alternative rejected:** Changing the visible `Name` to `Custom accent` or `Hint` to `color` would propagate to other UI locations or be less clear to sighted users. The accessible name diverging from the visible text is acceptable when it improves clarity.

### 4. Test Impact

The test file `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs` asserts `Active: Sky` and `Active: Custom` at lines 44, 191, 215, 239, 254. The `ActiveAccentLabel` format is unchanged, so no test updates are required.

No tests pin the theme names, descriptions, or the custom entry name/hint. The only asserted theme-related behavior is selection persistence, which is unaffected by copy changes.

**Decision:** No test changes required. The a11y-name change is a presentation concern (XAML binding and property implementation) without testable business logic beyond the existing selection tests.

### 5. README Screenshot

The existing screenshot at `docs/images/settings-appearance.png` is already stale (it predates recent changes). The user's brief notes that release-preparation owns screenshot refresh, and AGENTS.md specifies that a release sequence starts with `Capture-ReadmeImages.ps1` before tagging.

**Decision:** Do not refresh screenshots in this change. The screenshot will be updated in the release-preparation sequence.

### 6. Spec Delta Scope

The `preferences-ui` spec requirement `Application theme selector` (lines 279-307) quotes the theme names `Dark Theme`, `Light Theme`, `Deep Blue Theme`, `System Theme` and the four descriptions. These change, so a MODIFIED delta is required.

The `Accent color selector` requirement (lines 309-340) does not quote the custom entry's name or hint, and hex values remain visible. No delta is required for it.

The page description change (line 93 in `SettingsNavigation.cs`) is not pinned by the spec. The section-header removal is not spec-pinned. No deltas are required for those.

## Open Questions

None. All copy is approved, and the accessible-name strategy is determined.

## Deviations

None. The design follows the approved brief exactly.

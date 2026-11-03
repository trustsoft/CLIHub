# Proposal: Theme Asset Reorganization

## Why

The current `src/CLIHub/Themes/` directory mixes three concerns: palette XAML, shared-style XAML, and C# theme code. All 16 files—three palette dictionaries, eight style dictionaries, and five C# files—sit flat in one directory, making file roles harder to distinguish at a glance. The theme-asset domain is now stable and the directory has reached the natural size (16 files) where a one-level grouping clarifies rather than obscures.

This change groups palette XAML under `Themes/` (the primary capability), shared styles under `Themes/Common/`, and C# theme code under a new `Theming/` namespace folder. Benefits: palette files are immediately visible at the root of `Themes/`; styles are grouped without per-theme folders; C# code moves to its own cohesive namespace; and the layout remains a single level of nesting.

## What Changes

### File moves

**XAML files:**

```
src/CLIHub/Themes/DarkTheme.xaml    → src/CLIHub/Themes/Dark.xaml
src/CLIHub/Themes/LightTheme.xaml   → src/CLIHub/Themes/Light.xaml
src/CLIHub/Themes/BlueTheme.xaml    → src/CLIHub/Themes/Blue.xaml
src/CLIHub/Themes/ThemePickerData.xaml → src/CLIHub/Themes/Common/PickerData.xaml

src/CLIHub/Themes/Controls.xaml             → src/CLIHub/Themes/Common/Controls.xaml
src/CLIHub/Themes/LaunchWindowStyles.xaml   → src/CLIHub/Themes/Common/LaunchWindowStyles.xaml
src/CLIHub/Themes/SettingsControls.xaml     → src/CLIHub/Themes/Common/SettingsControls.xaml
src/CLIHub/Themes/SettingsStyles.xaml       → src/CLIHub/Themes/Common/SettingsStyles.xaml
src/CLIHub/Themes/Sizing.xaml               → src/CLIHub/Themes/Common/Sizing.xaml
src/CLIHub/Themes/SlimScrollBar.xaml        → src/CLIHub/Themes/Common/SlimScrollBar.xaml
src/CLIHub/Themes/WhatsNewStyles.xaml       → src/CLIHub/Themes/Common/WhatsNewStyles.xaml
```

**C# files:**

```
src/CLIHub/Themes/IconGlyphs.cs         → src/CLIHub/Theming/IconGlyphs.cs
src/CLIHub/Themes/PaletteSwitcher.cs    → src/CLIHub/Theming/PaletteSwitcher.cs
src/CLIHub/Themes/SystemThemeFollower.cs → src/CLIHub/Theming/SystemThemeFollower.cs
src/CLIHub/Themes/ThemeCatalog.cs       → src/CLIHub/Theming/ThemeCatalog.cs
src/CLIHub/Themes/ThemeDescriptors.cs   → src/CLIHub/Theming/ThemeDescriptors.cs
```

### Path and namespace updates

- WPF `ResourceDictionary Source` attributes: `/Themes/DarkTheme.xaml` becomes `/Themes/Dark.xaml`; `/Themes/SettingsStyles.xaml` becomes `/Themes/Common/SettingsStyles.xaml`, etc.
- `ThemeDescriptors.cs` palette paths: `/Themes/LightTheme.xaml` becomes `/Themes/Light.xaml`; `/Themes/BlueTheme.xaml` becomes `/Themes/Blue.xaml`.
- C# namespace changes from `CLIHub.Themes` to `CLIHub.Theming` in all five moved C# files.
- XAML `xmlns:themes="clr-namespace:CLIHub.Themes"` becomes `xmlns:themes="clr-namespace:CLIHub.Theming"` (14 files).
- C# `using CLIHub.Themes;` becomes `using CLIHub.Theming;` where needed.
- Test globs and allowlists update to match new names and locations.
- Documentation paths (`docs/architecture/ui.md`, `docs/repo-structure.md`) update to reflect the new structure.

### Unchanged

- File and type names for C# remain unchanged (`ThemeCatalog`, `ThemeDescriptors`, `PaletteSwitcher`, `SystemThemeFollower`, `IconGlyphs`).
- Palette resource keys, brush names, style keys, and all runtime behavior remain unchanged.
- System theme has no palette file of its own (it resolves to Dark or Light at runtime).
- `xmlns` prefix `themes` in XAML remains unchanged.
- All color, sizing, style, and palette logic stays identical.

## Impact

### Source files

- 11 XAML dictionaries: new Source paths.
- 5 C# theme files: new namespace `CLIHub.Theming`.
- 14 XAML files: `xmlns:themes` namespace updated to `clr-namespace:CLIHub.Theming`.
- 10 C# source files outside Themes: `using CLIHub.Themes;` → `using CLIHub.Theming;` (`App.xaml.cs`, `ServiceRegistration.cs`, `SettingsViewModel.cs`, `SettingsUpdateCard.cs`, `SettingsNavigation.cs`, `AccentSelection.cs`, `ApplicationSession.cs`, `WpfThemePreferenceTarget.cs`, `ShellCoordinator.cs`, `LaunchWindowActionBuilder.cs`).
- 1 XML doc comment: `CLIHub.Themes.PaletteSwitcher` → `CLIHub.Theming.PaletteSwitcher` (`AccentApplier.cs:24`).
- `src/CLIHub/CLIHub.csproj`: no change required (MSBuild discovers `.cs` and `.xaml` files automatically).

### Tests

- `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs`: glob changes from `Themes/*Theme.xaml` to `Themes/*.xaml` (root only, non-recursive).
- `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs`: two palette globs (lines 53, 116) change from `*Theme.xaml` to `*.xaml` at root; `Length >= 3` assertion kept.
- `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs`: allowlist and XML doc comments update to match `Themes/Dark.xaml`, `Themes/Light.xaml`, `Themes/Blue.xaml`, and `Themes/Common/PickerData.xaml`; line 49 assertion changes to `"Themes", "Common", "PickerData.xaml"`.
- `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs`: path assertion for `SettingsStyles.xaml` becomes `Themes/Common/SettingsStyles.xaml`.
- `tests/CLIHub.Tests/Themes/PaletteSwitcherTests.cs`: namespace import and two assertions (`LightTheme.xaml` → `Light.xaml`, `BlueTheme.xaml` → `Blue.xaml` at lines 40, 61).
- 6 test files total with namespace imports: `using CLIHub.Themes;` → `using CLIHub.Theming;` (`PaletteSwitcherTests.cs`, `SystemThemeFollowerTests.cs`, `ThemePreviewResourceKeyTests.cs`, `WpfThemePreferenceTargetTests.cs`, `ShellCoordinatorTests.cs`, `ApplicationSessionTests.cs`).
- No test weakening: assertions remain loud-failure, covering the same keys, paths, and parity checks.

### Documentation

- `docs/architecture/ui.md`: palette paths, merge tree, file references, Theme Data Locations table, C# type paths all update to the new structure; glob descriptions updated.
- `docs/repo-structure.md`: `Theming/` namespace folder mentioned alongside `Themes/`.
- `docs/architecture/startup.md:53`: `src/CLIHub/Themes/PaletteSwitcher.cs` → `src/CLIHub/Theming/PaletteSwitcher.cs`.
- `docs/backlog.md:367`: `src/CLIHub/Themes/SettingsStyles.xaml:214-230` → `src/CLIHub/Themes/Common/SettingsStyles.xaml:214-230`.
- `ui/mockups/settings-page-emblem.html`: palette and style paths update to new structure.
- `ui/mockups/README.md:48`: `DarkTheme.xaml`/`Sizing.xaml` paths updated.

### Build and verification

- No build breaks: all WPF pack URIs resolve through MSBuild component resource discovery.
- No runtime changes: palette, style, and namespace resolution work identically.
- Full rebuild and test suite must pass.

## Explicit Non-Goals

- Adding new theme palettes or accent presets.
- Refactoring theme logic or palette-switching behavior.
- Changing XAML resource keys, style names, or brush definitions.
- Creating per-theme subdirectories (e.g., `Themes/Dark/`).
- Reorganizing other asset directories (`Controls/`, `Views/`, `Assets/`).
- Changing behavior, UI appearance, or runtime palette resolution.
- Modifying the application configuration, accent handling, or theme follower logic.

## Dependencies

None. This change has no inter-change dependencies and may be implemented independently.

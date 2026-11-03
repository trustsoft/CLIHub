# Tasks: Theme Asset Reorganization

Implementation is organized into atomic stages with build/test gates. Every stage completes or reverts as one unit.

## Stage 1: Create directory structure and move files

- [x] 1.1 Create `src/CLIHub/Themes/Common/` directory
- [x] 1.2 Create `src/CLIHub/Theming/` directory
- [x] 1.3 Move and rename palette XAML files (filesystem move; git rename detection happens at commit):
  - Move `src/CLIHub/Themes/DarkTheme.xaml` → `src/CLIHub/Themes/Dark.xaml`
  - Move `src/CLIHub/Themes/LightTheme.xaml` → `src/CLIHub/Themes/Light.xaml`
  - Move `src/CLIHub/Themes/BlueTheme.xaml` → `src/CLIHub/Themes/Blue.xaml`
- [x] 1.4 Move style XAML files to Common/ (filesystem move):
  - Move `src/CLIHub/Themes/Controls.xaml` → `src/CLIHub/Themes/Common/Controls.xaml`
  - Move `src/CLIHub/Themes/LaunchWindowStyles.xaml` → `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml`
  - Move `src/CLIHub/Themes/SettingsControls.xaml` → `src/CLIHub/Themes/Common/SettingsControls.xaml`
  - Move `src/CLIHub/Themes/SettingsStyles.xaml` → `src/CLIHub/Themes/Common/SettingsStyles.xaml`
  - Move `src/CLIHub/Themes/Sizing.xaml` → `src/CLIHub/Themes/Common/Sizing.xaml`
  - Move `src/CLIHub/Themes/SlimScrollBar.xaml` → `src/CLIHub/Themes/Common/SlimScrollBar.xaml`
  - Move `src/CLIHub/Themes/WhatsNewStyles.xaml` → `src/CLIHub/Themes/Common/WhatsNewStyles.xaml`
- [x] 1.5 Move and rename ThemePickerData.xaml (filesystem move):
  - Move `src/CLIHub/Themes/ThemePickerData.xaml` → `src/CLIHub/Themes/Common/PickerData.xaml`
- [x] 1.6 Move C# files to Theming/ (filesystem move):
  - Move `src/CLIHub/Themes/IconGlyphs.cs` → `src/CLIHub/Theming/IconGlyphs.cs`
  - Move `src/CLIHub/Themes/PaletteSwitcher.cs` → `src/CLIHub/Theming/PaletteSwitcher.cs`
  - Move `src/CLIHub/Themes/SystemThemeFollower.cs` → `src/CLIHub/Theming/SystemThemeFollower.cs`
  - Move `src/CLIHub/Themes/ThemeCatalog.cs` → `src/CLIHub/Theming/ThemeCatalog.cs`
  - Move `src/CLIHub/Themes/ThemeDescriptors.cs` → `src/CLIHub/Theming/ThemeDescriptors.cs`

**Gate: Directory structure is correct**
- [x] 1.7 Verify directory layout:
  - `src/CLIHub/Themes/` contains exactly 3 XAML files: `Dark.xaml`, `Light.xaml`, `Blue.xaml`
  - `src/CLIHub/Themes/Common/` contains exactly 8 XAML files: `Controls.xaml`, `LaunchWindowStyles.xaml`, `PickerData.xaml`, `SettingsControls.xaml`, `SettingsStyles.xaml`, `SlimScrollBar.xaml`, `Sizing.xaml`, `WhatsNewStyles.xaml`
  - `src/CLIHub/Theming/` contains exactly 5 C# files: `IconGlyphs.cs`, `PaletteSwitcher.cs`, `SystemThemeFollower.cs`, `ThemeCatalog.cs`, `ThemeDescriptors.cs`
  - `src/CLIHub/Themes/` contains no C# files

## Stage 2: Update all compile-affecting changes (C#, XAML namespaces, ResourceDictionary paths)

- [x] 2.1 Update namespace declarations in moved C# files (5 files):
  - `src/CLIHub/Theming/IconGlyphs.cs:1` — `namespace CLIHub.Themes;` → `namespace CLIHub.Theming;`
  - `src/CLIHub/Theming/PaletteSwitcher.cs:1` — `namespace CLIHub.Themes;` → `namespace CLIHub.Theming;`
  - `src/CLIHub/Theming/SystemThemeFollower.cs:1` — `namespace CLIHub.Themes;` → `namespace CLIHub.Theming;`
  - `src/CLIHub/Theming/ThemeCatalog.cs:1` — `namespace CLIHub.Themes;` → `namespace CLIHub.Theming;`
  - `src/CLIHub/Theming/ThemeDescriptors.cs:1` — `namespace CLIHub.Themes;` → `namespace CLIHub.Theming;`
- [x] 2.2 Update ThemeDescriptors.cs palette paths (2 sites):
  - Line 57: `/Themes/LightTheme.xaml` → `/Themes/Light.xaml`
  - Line 64: `/Themes/BlueTheme.xaml` → `/Themes/Blue.xaml`
- [x] 2.3 Update using directives in source C# files (10 files):
  - `src/CLIHub/App.xaml.cs:11` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/Composition/ServiceRegistration.cs:23` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:9` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/ViewModels/Settings/SettingsUpdateCard.cs:4` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:3` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/ViewModels/Settings/AccentSelection.cs:4` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/Lifetime/ApplicationSession.cs:6` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/Settings/WpfThemePreferenceTarget.cs:7` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/Startup/ShellCoordinator.cs:6` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `src/CLIHub/ViewModels/Launch/LaunchWindowActionBuilder.cs:5` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
- [x] 2.4 Update XML doc comment reference:
  - `src/CLIHub/Settings/AccentApplier.cs:24` — `CLIHub.Themes.PaletteSwitcher` → `CLIHub.Theming.PaletteSwitcher`
- [x] 2.5 Update xmlns:themes in all 14 XAML files:
  - `xmlns:themes="clr-namespace:CLIHub.Themes"` → `xmlns:themes="clr-namespace:CLIHub.Theming"`
  - Files (grep verified): `Views/LaunchWindow.xaml:6`, `Views/WhatsNewWindow.xaml:6`, `Views/SettingsWindow.xaml:7`, `Views/SettingsAgentDetectionPage.xaml:7`, `Views/SettingsAgentsPage.xaml:8`, `Views/SettingsAppearancePage.xaml:7`, `Views/SettingsGeneralPage.xaml:7`, `Views/SettingsLaunchWindowPage.xaml:8`, `Views/SettingsLogsPage.xaml:8`, `Views/SettingsProjectRegistryPage.xaml:7`, `Views/SettingsProjectsPage.xaml:8`, `Views/SettingsTerminalPage.xaml:8`, `Views/SettingsUpdatesPage.xaml:7`, `Themes/Common/SettingsControls.xaml:4`
- [x] 2.6 Update App.xaml ResourceDictionary Source paths (3 sites):
  - Line 11: `/Themes/DarkTheme.xaml` → `/Themes/Dark.xaml`
  - Line 17: `/Themes/SlimScrollBar.xaml` → `/Themes/Common/SlimScrollBar.xaml`
  - Line 30: `/Themes/SettingsStyles.xaml` → `/Themes/Common/SettingsStyles.xaml`
- [x] 2.7 Update window XAML ResourceDictionary Source paths (2 files):
  - `Views/LaunchWindow.xaml` line 31: `/Themes/LaunchWindowStyles.xaml` → `/Themes/Common/LaunchWindowStyles.xaml`
  - `Views/WhatsNewWindow.xaml`: `/Themes/WhatsNewStyles.xaml` → `/Themes/Common/WhatsNewStyles.xaml`
- [x] 2.8 Update style dictionary merges in Themes/Common/ (6 files that declare merges):
  - `SettingsStyles.xaml`: `/Themes/Controls.xaml` → `/Themes/Common/Controls.xaml`; `/Themes/ThemePickerData.xaml` → `/Themes/Common/PickerData.xaml`; `/Themes/SettingsControls.xaml` → `/Themes/Common/SettingsControls.xaml`
  - `Controls.xaml`: `/Themes/Sizing.xaml` → `/Themes/Common/Sizing.xaml`
  - `LaunchWindowStyles.xaml`: `/Themes/Controls.xaml` → `/Themes/Common/Controls.xaml`
  - `WhatsNewStyles.xaml`: `/Themes/Controls.xaml` → `/Themes/Common/Controls.xaml`
  - `PickerData.xaml`: `/Themes/Sizing.xaml` → `/Themes/Common/Sizing.xaml`
  - `SettingsControls.xaml`: `/Themes/Sizing.xaml` → `/Themes/Common/Sizing.xaml`

**Gate: Build succeeds**
- [x] 2.9 Run `dotnet build CLIHub.sln` — must report 0 errors, 0 warnings

## Stage 3: Update test files

- [x] 3.1 Update ThemePaletteParityTests.cs (tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs):
  - Line 13: Glob `"*Theme.xaml"` → `"*.xaml"` (matches only root files, `Common/` excluded by non-recursive enumeration)
  - Line 19: Assertion `"DarkTheme.xaml"` → `"Dark.xaml"`
  - Line 20: Assertion `"LightTheme.xaml"` → `"Light.xaml"`
  - Add explicit assertion for `"Blue.xaml"` presence
- [x] 3.2 Update ApplicationResourceKeyTests.cs (tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs):
  - Line 53: Glob `"*Theme.xaml"` → `"*.xaml"`
  - Line 116: Glob `"*Theme.xaml"` → `"*.xaml"`
  - Keep `paletteFiles.Length >= 3` loud-failure assertion
- [x] 3.3 Update ThemeColorLiteralTests.cs (tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs):
  - Lines 14-15: XML doc comment `Themes/*Theme.xaml` → `Themes/*.xaml`; `Themes/ThemePickerData.xaml` → `Themes/Common/PickerData.xaml`
  - Line 49: Assertion path `"Themes", "ThemePickerData.xaml"` → `"Themes", "Common", "PickerData.xaml"`
  - Line 103: Allowlist path `"Themes/ThemePickerData.xaml"` → `"Themes/Common/PickerData.xaml"`
  - Line 108: Pattern `"^Themes/[^/]+Theme\\.xaml$"` → `"^Themes/(Dark|Light|Blue)\\.xaml$"`
- [x] 3.4 Update XamlResourceReferenceTests.cs (tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs):
  - Line 92: Assertion path `"Themes", "SettingsStyles.xaml"` → `"Themes", "Common", "SettingsStyles.xaml"`
- [x] 3.5 Update PaletteSwitcherTests.cs (tests/CLIHub.Tests/Themes/PaletteSwitcherTests.cs):
  - Line 6: `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - Line 40: Assertion `"LightTheme.xaml"` → `"Light.xaml"`
  - Line 61: Assertion `"BlueTheme.xaml"` → `"Blue.xaml"`
- [x] 3.6 Update test namespace imports (5 additional test files):
  - `tests/CLIHub.Tests/Themes/SystemThemeFollowerTests.cs:8` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `tests/CLIHub.Tests/Themes/ThemePreviewResourceKeyTests.cs:5` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs:7` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `tests/CLIHub.Tests/Services/ShellCoordinatorTests.cs:14` — `using CLIHub.Themes;` → `using CLIHub.Theming;`
  - `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs:13` — `using CLIHub.Themes;` → `using CLIHub.Theming;`

**Gate: All tests pass**
- [x] 3.7 Run `dotnet test CLIHub.sln` — must report 0 failed tests

## Stage 4: Update documentation

- [x] 4.1 Update docs/architecture/ui.md:
  - Line 11 reference: `/Themes/DarkTheme.xaml` → `/Themes/Dark.xaml`
  - Line 14 layering recipe paragraph: `ThemePickerData.xaml` → `Common/PickerData.xaml`; `SettingsControls.xaml` → `Common/SettingsControls.xaml`; `SettingsStyles.xaml` → `Common/SettingsStyles.xaml`; `LaunchWindowStyles.xaml` → `Common/LaunchWindowStyles.xaml`; `WhatsNewStyles.xaml` → `Common/WhatsNewStyles.xaml`
  - Line 20 reference: `DarkTheme.xaml` → `Dark.xaml` and the nested `LightTheme/BlueTheme` names → `Light/Blue`
  - Line 21 reference: `SlimScrollBar.xaml` → `Common/SlimScrollBar.xaml`
  - Line 22 reference: `SettingsStyles.xaml` → `Common/SettingsStyles.xaml`
  - Lines 23-28 merge tree: update all paths to include `Common/` for styles
  - Line 31 reference: `Themes/<Window>Styles.xaml` → `Themes/Common/<Window>Styles.xaml` (plus all specific dictionary names in that recipe)
  - Line 34 reference: `/Themes/Controls.xaml` → `/Themes/Common/Controls.xaml`
  - Line 121: Palette names `DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml` → `Dark.xaml`, `Light.xaml`, `Blue.xaml`
  - Line 126: Recipe "Copy `DarkTheme.xaml` to a new `<Name>Theme.xaml`" → "Copy `Dark.xaml` to a new `<Name>.xaml`"
  - Line 127: C# paths `src/CLIHub/Themes/ThemeDescriptors.cs` → `src/CLIHub/Theming/ThemeDescriptors.cs`
  - Line 128: `ThemePickerData.xaml` → `Themes/Common/PickerData.xaml`
  - Line 129: Glob description `Themes/*Theme.xaml` → `Themes/*.xaml` (root only); `Themes/ThemePickerData.xaml` → `Themes/Common/PickerData.xaml`
  - Line 151: `ThemePickerData.xaml` → `Themes/Common/PickerData.xaml`
  - Lines 138-141 Theme Data Locations table: update all file paths to reflect new structure (`src/CLIHub/Theming/` for C# files, `Themes/Dark.xaml` etc. for palettes, `Themes/Common/PickerData.xaml`)
  - Sweep the whole file for any remaining `DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml`, `ThemePickerData.xaml`, `Themes/ThemeDescriptors.cs`, `Themes/ThemeCatalog.cs`, `Themes/PaletteSwitcher.cs`, `Themes/SystemThemeFollower.cs`, `Themes/SettingsStyles.xaml`, `Themes/Controls.xaml`, or `Themes/Sizing.xaml` references and update them to the new paths
- [x] 4.2 Update docs/repo-structure.md:
  - Line 55: Add `Theming/` namespace folder mention alongside `Themes/`
- [x] 4.3 Update docs/architecture/startup.md:
  - Line 53: `src/CLIHub/Themes/PaletteSwitcher.cs` → `src/CLIHub/Theming/PaletteSwitcher.cs`
- [x] 4.4 Update docs/backlog.md:
  - Line 367: `src/CLIHub/Themes/SettingsStyles.xaml:214-230` → `src/CLIHub/Themes/Common/SettingsStyles.xaml:214-230`
- [x] 4.5 Update ui/mockups/settings-page-emblem.html:
  - Lines 9-10, 20: `Themes/DarkTheme.xaml` → `Themes/Dark.xaml`; `Themes/Sizing.xaml` → `Themes/Common/Sizing.xaml`; `Themes/SettingsStyles.xaml` → `Themes/Common/SettingsStyles.xaml`
- [x] 4.6 Update ui/mockups/README.md:
  - Line 48: `Themes/DarkTheme.xaml` → `Themes/Dark.xaml`; `Themes/Sizing.xaml` → `Themes/Common/Sizing.xaml`

**Gate: Documentation is accurate**
- [x] 4.7 Verify all path references in documentation match the new structure

## Stage 5: Final verification

- [x] 5.1 Run full build: `dotnet build CLIHub.sln` — 0 errors, 0 warnings
- [x] 5.2 Run full test suite: `dotnet test CLIHub.sln` — 0 failed tests
- [x] 5.3 Run OpenSpec validation: `openspec validate --all` — must report success
- [x] 5.4 Update knowledge graph: `graphify update .`
- [x] 5.5 Verify directory structure:
  - `src/CLIHub/Themes/` contains exactly 3 files: `Dark.xaml`, `Light.xaml`, `Blue.xaml`
  - `src/CLIHub/Themes/Common/` contains exactly 8 files: `Controls.xaml`, `LaunchWindowStyles.xaml`, `PickerData.xaml`, `SettingsControls.xaml`, `SettingsStyles.xaml`, `SlimScrollBar.xaml`, `Sizing.xaml`, `WhatsNewStyles.xaml`
  - `src/CLIHub/Theming/` contains exactly 5 files: `IconGlyphs.cs`, `PaletteSwitcher.cs`, `SystemThemeFollower.cs`, `ThemeCatalog.cs`, `ThemeDescriptors.cs`
  - `src/CLIHub/Themes/` directory contains no C# files

## Notes

- File moves are filesystem operations (Move-Item on Windows); git rename detection happens in the orchestrator's commit, preserving file history.
- Build gates catch WPF pack URI resolution failures immediately.
- Test gates verify that glob/allowlist updates are correct.
- The change is purely structural: no behavior, appearance, or runtime logic changes.
- Stages 1-2 touch source files; Stage 3 touches tests; Stage 4 updates docs; Stage 5 is final verification.
- If any stage fails its gate, revert that stage before continuing.

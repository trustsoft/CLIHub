# Theme registry with a full blue palette

## Why

The theme mechanism is hardcoded to exactly two palettes. `App.ApplyTheme()`
(`src/CLIHub/App.xaml.cs:41-54`) special-cases "light" by merging `LightTheme.xaml` on top of the
`LaunchTheme.xaml` base that `App.xaml` merges unconditionally; every other mode silently means
"dark". Adding a third palette today would mean another `if` branch, and the parity test
(`tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:11-12`) hardcodes exactly two file paths.
The base file's name, `LaunchTheme.xaml`, is a historical leftover from when the palette belonged to
the launch window only (see `openspec/changes/archive/2026-09-30-launch-window-ui/`), and it now
misleads: the palette is application-wide (`docs/architecture/ui.md:13`, `App.xaml:10`).

A blue palette is wanted as a third theme, and the mechanism should make a future fourth theme a
data change instead of a logic change.

## What Changes

- **Theme registry.** New `src/CLIHub/Themes/ThemeCatalog.cs`: a static registry mapping a resolved
  `AppTheme` to its palette dictionary path (`"/Themes/DarkTheme.xaml"`, `"/Themes/LightTheme.xaml"`,
  `"/Themes/BlueTheme.xaml"`). `App.ApplyTheme()` becomes generic: parse the stored token, resolve
  the registry entry (System resolves to dark or light via `SystemTheme.IsLight()`, never to blue),
  and merge that palette. Dark stays the base palette merged by `App.xaml`, so a missing or unknown
  token still renders dark with no extra merge.
- **Core token.** `AppTheme` gains a `Blue` member and `AppThemes` gains `BlueToken = "blue"`
  (`TryParse`/`ToToken`). Core learns only the token; the palette mapping stays in the UI project
  (Core has no WPF dependency).
- **Full blue palette.** New `src/CLIHub/Themes/BlueTheme.xaml` with the same 28-brush key set as
  the dark palette: blue-tinted dark chrome surfaces, cool-tinted text tones, and a baked-in
  `AccentBrush` `#FF3B82F6` (the existing Blue accent-preset family) with a matching
  `AccentSelectionBrush` `#1A3B82F6`. A stored non-default accent still overrides the accent brushes
  live, unchanged.
- **Rename.** `src/CLIHub/Themes/LaunchTheme.xaml` → `src/CLIHub/Themes/DarkTheme.xaml`; the three
  in-code references (`App.xaml:11`, `LaunchWindow.xaml:35` comment, parity test) and the
  `LightTheme.xaml` header comment are updated.
- **Settings.** The Appearance page's application-theme selector offers a fourth preview card,
  "Blue Theme", with its own palette-preview brushes and triggers; option order becomes Dark, Light,
  Blue, System.
- **Parity test generalized.** `ThemePaletteParityTests` enumerates every
  `src/CLIHub/Themes/*Theme.xaml` and asserts one identical key set, so a new palette file joins the
  check automatically.
- **Docs.** `docs/architecture/ui.md` theme recipes (lines 11-19 and 66-71) are rewritten for the
  registry ("add a theme" = 1 XAML file + 1 registry entry + 1 settings option + preview brushes);
  the README's "shared Dark/Light theme" line mentions Blue.

Theme switching stays restart-based; accent switching stays live. The durable behavior contract is
updated in `application-theme`, `preferences-ui`, and `launch-window-theme`.

## Impact

- Affected specs:
  - `application-theme` (MODIFIED: *Theme mode preference* — Blue offered and stored; *Theme palette
    resolution* — registry resolution, a blue palette scenario, key-completeness, System never blue;
    *Accent color preference* — the default accent follows the active palette's baked-in accent)
  - `preferences-ui` (MODIFIED: *Application theme selector* — four preview cards)
  - `launch-window-theme` (MODIFIED: *Dark launch window theme* — the blue palette surfaces scenario)
- Affected code:
  - `src/CLIHub.Core/Models/Appearance/AppTheme.cs` (enum + token registry)
  - `tests/CLIHub.Core.Tests/Models/Appearance/AppThemesTests.cs` (known-token coverage)
  - `src/CLIHub/Themes/LaunchTheme.xaml` → `DarkTheme.xaml` (rename, content unchanged)
  - new `src/CLIHub/Themes/BlueTheme.xaml`, new `src/CLIHub/Themes/ThemeCatalog.cs`
  - `src/CLIHub/App.xaml` (merge path) and `App.xaml.cs` (`ApplyTheme` rewrite)
  - `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (`AppThemeOptions` entry),
    `src/CLIHub/Themes/SettingsStyles.xaml` (preview brushes),
    `src/CLIHub/Views/SettingsAppearancePage.xaml` (preview triggers)
  - `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs` (generalized enumeration)
  - `src/CLIHub/Views/LaunchWindow.xaml` (comment only), `docs/architecture/ui.md`, `README.md`
- No configuration-document change: `preferences.appTheme` stays a string token; `"dark"`,
  `"light"`, and `"system"` keep parsing, `"blue"` is added, unknown values keep falling back to
  dark (`AppConfigDocument.cs:135,167,209`).

## Non-goals

- Live (restart-free) theme switching; the "Theme applies on next start" requirement is preserved.
- Accent variants of existing palettes: Blue is a full palette, not a recolored dark theme.
- Deriving the settings option list from the registry: the settings copy stays independent of the
  palette mapping for now (single small duplication, noted in design.md).
- New palettes beyond blue.
- System mode resolving to blue: System remains a Windows-driven dark/light two-way sync.
- Rewriting historical references to `LaunchTheme.xaml` inside `openspec/changes/archive/` (history
  is not edited).

## Dependencies

- None on other open changes. Builds on the archived `settings-theme-mode` change's merge-on-top
  palette mechanism and the `theme-palette-dynamic-refactor` change's `DynamicResource` resolution,
  both already in the main specs.

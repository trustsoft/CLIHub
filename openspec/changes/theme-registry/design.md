# Design — theme registry with a full blue palette

## Verified current state (as of 2026-10-20, commit 7caed41, clean tree)

- `src/CLIHub/Themes/LaunchTheme.xaml:1-54` — the dark palette: 28 `SolidColorBrush` keys in five
  comment groups. Surfaces: `ChromeBgBrush` `#FF26262C`, `StripBgBrush` `#24000000`,
  `PopoverBgBrush` `#FF2B2B31`, `PopoverOpenBrush` `#FF1E1E23`, `HairlineBrush` `#0FFFFFFF`.
  Strokes/overlays: `FieldStrokeBrush`, `ActionsBorderBrush`, `ControlStrokeBrush`, `ChipBgBrush`,
  `ChipStrokeBrush`, `ThumbBgBrush`, `RowDividerBrush`, `HoverOverlayBrush`, `PressedOverlayBrush`,
  `AccentSelectionBrush` `#1A4CC2FF`, `TopHighlightBrush`, `ScrollThumbBrush`. Text:
  `TextPrimaryBrush`, `TextMutedBrush`, `SectionLabelBrush`, `BrandBrush`, `IconBrush`,
  `MenuItemBrush`, `DisabledTextBrush`. Accent: `AccentBrush` `#FF4CC2FF`. Status: `DangerBrush`,
  `SuccessBrush`, `SuccessSelectionBrush`.
- `src/CLIHub/Themes/LightTheme.xaml:1-47` — the same 28 keys with light values; header comment
  names LaunchTheme.xaml. Its baked accent is the same Sky `#FF4CC2FF` (dark and light share the
  accent today; blue will not).
- `src/CLIHub/App.xaml:11` merges `/Themes/LaunchTheme.xaml` as the base palette, plus
  `Controls.xaml`, `DarkScrollBar.xaml`, `SettingsStyles.xaml` (lines 13-26).
- `src/CLIHub/App.xaml.cs:41-54` — `ApplyTheme()` parses the stored token
  (`AppThemes.Parse(PreferenceReader.ReadAppTheme(...))`), computes
  `useLight = theme == AppTheme.Light || (theme == AppTheme.System && SystemTheme.IsLight())`, and
  appends `pack://application:,,,/CLIHub;component/Themes/LightTheme.xaml` to
  `Resources.MergedDictionaries` when light. Dark (and parse failures) keep the App.xaml base only.
  `ApplyAccent()` at lines 60-70 overrides `AccentBrush`/`AccentSelectionBrush` application
  resources only when the stored accent differs from `AccentPalette.DefaultHex` (`#4CC2FF`).
- `src/CLIHub.Core/Models/Appearance/AppTheme.cs:6-22` — `enum AppTheme { Dark, Light, System }`;
  `AppThemes` (lines 27-93) with `DarkToken`/`LightToken`/`SystemToken`, case-insensitive
  whitespace-trimmed `TryParse` (false on null/empty/unknown), `Parse` falling back to Dark, and
  `ToToken`.
- `src/CLIHub.Core/Models/Appearance/AccentPalette.cs:18-36` — `DefaultHex = "#4CC2FF"` and the 8
  presets including Blue `#3B82F6`; `SelectionTint`/`Opaque` helpers (lines 86-93).
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:338-343` — `AppThemeOptions`: Dark Theme,
  Light Theme, System Sync with descriptions; `selectedAppTheme` field (line 414), `AppThemes`
  property (line 791), draft loading `FirstOrDefault(o => o.Theme == draft.AppTheme) ??
  AppThemeOptions[0]` (line 1290), save passes `SelectedAppTheme.Theme` (line 1849).
- `src/CLIHub/Views/SettingsAppearancePage.xaml:17-123` — the theme selector `ListBox` bound to
  `AppThemes`; each card's palette preview is a hardcoded `Border` background switched by
  `DataTrigger`s on `models:AppTheme.Light`/`System` (lines 38-43, 70-77) against
  `ThemePreviewDarkBrush`/`ThemePreviewLightBrush`/`ThemePreviewSystemBrush` and
  `ThemePreviewBarDarkBrush`/`ThemePreviewBarLightBrush`, defined in
  `src/CLIHub/Themes/SettingsStyles.xaml:363-374`. The card hint (line 14) says "Applies after the
  next start; System Sync follows Windows."
- `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:8-28` — reads exactly
  `Themes/LaunchTheme.xaml` and `Themes/LightTheme.xaml` by path, extracts `x:Key` values by regex,
  asserts equal key sets.
- `tests/CLIHub.Core.Tests/Models/Appearance/AppThemesTests.cs:1-33` — unknown→dark theory,
  known-token theory (`Dark`/`light`/`System`), round-trip over `Enum.GetValues<AppTheme>()`.
- Persistence: `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:135`
  `public string AppTheme { get; set; } = "dark";` (camelCase `preferences.appTheme` on disk;
  `PreferenceReader.ReadAppTheme` reads it at `src/CLIHub.Core/Logging/PreferenceReader.cs:57-58`),
  mapped through `AppThemes.ToToken` (line 167) and `AppThemes.Parse` (line 209). `AccentColor`
  default `"#4CC2FF"` (line 136). The theme flows Settings → `SettingsInput.AppTheme` →
  `SettingsDraft.AppTheme` (`src/CLIHub/Settings/SettingsApplicationService.cs:53,104,303,481`) →
  `preferences.AppTheme`; `ConfigurationSnapshot.cs:90` clones it. All steps carry the enum
  generically, so a new member needs no plumbing changes.
- `src/CLIHub.Core.Windows/SystemTheme.cs:17-28` — `IsLight()` reads
  `HKCU\...\Themes\Personalize\AppsUseLightTheme`, failures yield false (dark).
- Docs: `docs/architecture/ui.md:13` (XAML layering naming LaunchTheme.xaml), `:66-71` ("The dark
  and light palettes ... define the same brush keys" + "To add another palette" 2-step recipe).
  `README.md:46` "shared Dark/Light theme". `src/CLIHub/Views/LaunchWindow.xaml:35` comment names
  LaunchTheme.xaml.
- No `.openspec.yaml` convention in existing changes; deltas use `# <capability> (partial)` +
  `## MODIFIED Requirements` (see `openspec/changes/archive/2026-10-20-git-bash-resolution/`).

## Decisions

### D1 — Theme identity stays a Core enum + token registry; palette mapping lives in the UI project

`AppTheme` gains `Blue` (placed between `Light` and `System`, so the enum reads "explicit palettes
first, sync mode last"; nothing persists numeric values). `AppThemes` gains
`public const string BlueToken = "blue"`, a `case "blue":` arm, and a `ToToken` arm. The round-trip
test covers Blue automatically via `Enum.GetValues<AppTheme>()`; the known-token theory gains an
explicit `"blue"` row.

The id → palette mapping cannot live in Core (pack URIs are a WPF application concern; Core has no
WPF dependency). It becomes `src/CLIHub/Themes/ThemeCatalog.cs`:

```csharp
/// <summary>
///   Registry mapping a resolved application theme to its palette dictionary. The dark palette is
///   the base palette merged by App.xaml; a null resolution keeps that base.
/// </summary>
public static class ThemeCatalog
{
    public const string DarkPalettePath = "/Themes/DarkTheme.xaml";

    public static string? ResolvePalettePath(AppTheme theme, bool systemPrefersLight) => theme switch
    {
        AppTheme.Light => "/Themes/LightTheme.xaml",
        AppTheme.Blue => "/Themes/BlueTheme.xaml",
        AppTheme.System => systemPrefersLight ? "/Themes/LightTheme.xaml" : null,
        _ => null
    };
}
```

`App.ApplyTheme()` becomes:

```csharp
var theme = AppThemes.Parse(PreferenceReader.ReadAppTheme(AppPaths.ConfigFile));
var palettePath = ThemeCatalog.ResolvePalettePath(theme, SystemTheme.IsLight());
if (palettePath is null || Current is not { } application)
{
    return;
}
application.Resources.MergedDictionaries.Add(new ResourceDictionary
{
    Source = new Uri($"pack://application:,,,/CLIHub;component{palettePath}", UriKind.Absolute)
});
```

Null means "keep the App.xaml base (dark)": explicit Dark, System-on-Windows-dark, and every parse
failure (missing/unknown token, per the preserved "Unknown theme falls back to dark" scenario) take
the same path. Dark-on-dark merging is avoided; Light and Blue merge on top exactly like today's
light merge (last-wins `DynamicResource` lookup). `ApplyAccent()` is untouched.

Rationale: "adding a theme" becomes one XAML file + one `ResolvePalettePath` arm + one settings
option, with no conditional growth in `App.xaml.cs`.

**Alternatives rejected**

- *Registry of full descriptor records (`AppTheme`, name, description, `PaletteUri`) driving both
  palette resolution and the settings options list*: merges two concerns with different lifecycles
  (settings copy is user-facing wording; palette mapping is startup resolution). The user decision
  fixed "1 registry entry + 1 settings option" as separate edits; if a fourth palette arrives, the
  catalog can be extended then.
- *Registry returning `ResourceDictionary` instances*: creating WPF resources inside a static
  initializer hurts testability and design-time tooling; strings keep the type WPF-free even though
  the file sits in the UI project.
- *Placing the catalog in Core with plain string paths*: the paths are meaningless without WPF pack
  resolution, and Core must stay UI-independent (`docs/project-context.md:32-34`).
- *Replacing the App.xaml base merge with pure code-resolved palettes*: removes the design-time and
  fail-safe default; the merged base is what guarantees dark rendering when anything above startup
  resolution fails.

### D2 — Blue is a full palette with a baked-in Blue accent

`BlueTheme.xaml` defines the identical 28-key set. Principle of derivation: start from the dark
palette's neutral `#26262C` chrome, shift the neutral surfaces toward blue (hue ≈ 218°) while
keeping the same lightness steps, keep alpha-white/alpha-black strokes and overlays hue-neutral
(they read identically on any dark surface, and `LightTheme.xaml` shows the same pattern with
alpha-black), cool-shift the text greys slightly, and bake in the existing Blue accent preset
`#3B82F6` (`AccentPalette.cs:29`) instead of Sky.

Proposed values (each marked for user review — open question Q1):

| Key | Dark (current) | Blue (proposed) | Rationale |
|---|---|---|---|
| `ChromeBgBrush` | `#FF26262C` | `#FF232833` | blue-shifted chrome, one step darker, hue ≈ 218° |
| `StripBgBrush` | `#24000000` | `#24000000` | hue-neutral translucent overlay, unchanged |
| `PopoverBgBrush` | `#FF2B2B31` | `#FF2A3140` | one step lighter than chrome, blue |
| `PopoverOpenBrush` | `#FF1E1E23` | `#FF1C212B` | pressed/open tone, darker blue |
| `HairlineBrush` | `#0FFFFFFF` | `#0FFFFFFF` | unchanged |
| `FieldStrokeBrush` | `#1AFFFFFF` | `#1AFFFFFF` | unchanged |
| `ActionsBorderBrush` | `#FF38383C` | `#FF353C4A` | blue-tinted opaque border |
| `ControlStrokeBrush` | `#1FFFFFFF` | `#1FFFFFFF` | unchanged |
| `ChipBgBrush` | `#0DFFFFFF` | `#0DFFFFFF` | unchanged |
| `ChipStrokeBrush` | `#14FFFFFF` | `#14FFFFFF` | unchanged |
| `ThumbBgBrush` | `#1AFFFFFF` | `#1AFFFFFF` | unchanged |
| `RowDividerBrush` | `#0AFFFFFF` | `#0AFFFFFF` | unchanged |
| `HoverOverlayBrush` | `#0FFFFFFF` | `#0FFFFFFF` | unchanged |
| `PressedOverlayBrush` | `#14000000` | `#14000000` | unchanged |
| `AccentSelectionBrush` | `#1A4CC2FF` | `#1A3B82F6` | baked accent's selection tint (dark uses the `1A` alpha) |
| `TopHighlightBrush` | `#0DFFFFFF` | `#0DFFFFFF` | unchanged |
| `ScrollThumbBrush` | `#33FFFFFF` | `#33FFFFFF` | unchanged |
| `TextPrimaryBrush` | `#FFE9E9E9` | `#FFE8EBF1` | cool off-white; ≈12.4:1 on `#232833` |
| `TextMutedBrush` | `#FF9A9A9A` | `#FF9BA4B5` | ≈5.9:1 (AA normal text) |
| `SectionLabelBrush` | `#FF8B8B8B` | `#FF8D97A9` | ≈5.0:1 |
| `BrandBrush` | `#FFB0B0B0` | `#FFADB6C6` | between muted and primary |
| `IconBrush` | `#FFD8D8D8` | `#FFD7DCE6` | near-primary |
| `MenuItemBrush` | `#FFE6E6E6` | `#FFE5E9F1` | menu entries stay a hair under primary |
| `DisabledTextBrush` | `#FF5C5C63` | `#FF5A6272` | intentionally low contrast |
| `AccentBrush` | `#FF4CC2FF` | `#FF3B82F6` | baked Blue preset accent; ≈4.0:1 on chrome (UI-component AA, same standing as the preset has today on the neutral chrome) |
| `DangerBrush` | `#FFD97B7B` | `#FFD98282` | error tone slightly cooled for the blue surface |
| `SuccessBrush` | `#FF4ADE80` | `#FF4ADE80` | unchanged; green reads on blue as on neutral |
| `SuccessSelectionBrush` | `#1F4ADE80` | `#1F4ADE80` | unchanged |

Accent interplay (verified against `App.xaml.cs:60-70`): `ApplyAccent()` overrides
`AccentBrush`/`AccentSelectionBrush` only when the stored accent differs from `#4CC2FF`. So under
Blue with the default accent, the baked `#FF3B82F6` shows; any stored non-default accent (Sky
explicitly, or any other preset/custom) overrides it exactly as today. A user who explicitly stored
Sky sees Sky on Blue; a user who never touched accent sees the blue palette's own accent. This is
the "baked-in accent" the user chose, and the spec delta records it under *Accent color
preference*.

### D3 — Settings selector: four cards, preview brushes extended

`AppThemeOptions` gains `new(AppTheme.Blue, "Blue Theme", "Blue-tinted dark chrome with a baked-in
blue accent")` after Light, before System. The Appearance page preview `Border` gains
`DataTrigger`s on `models:AppTheme.Blue` in both trigger blocks
(`SettingsAppearancePage.xaml:38-43`, `70-77`), backed by new
`ThemePreviewBlueBrush` `#FF232833` and `ThemePreviewBarBlueBrush` `#FF3E4C66` in
`SettingsStyles.xaml` next to the existing preview brushes (lines 363-374). The card hint stays
(its "System Sync follows Windows" claim remains required by the *Restart note* scenario).

### D4 — Parity test generalizes by file glob

`ThemePaletteParityTests` enumerates `src/CLIHub/Themes/*Theme.xaml` (search pattern
`*Theme.xaml`), asserts at least two files and that every file's `x:Key` set equals the first
file's. `BlueTheme.xaml` then joins the check with no test edit, and a future palette cannot
silently miss a key. The file list is asserted to contain `DarkTheme.xaml` and `LightTheme.xaml`
explicitly so a rename or glob accident fails loudly rather than passing vacuously.

### D5 — Rename LaunchTheme.xaml → DarkTheme.xaml

`git mv` (history-preserving), then update the only live references: `App.xaml:11`, the parity
test, `LightTheme.xaml:5` and the renamed file's own header comment, and the
`LaunchWindow.xaml:35` comment. Archived changes and `docs` history are not rewritten.

### D6 — Docs recipes

`docs/architecture/ui.md` line 13 layering is rewritten around `DarkTheme.xaml` as base +
`ThemeCatalog` registry; lines 66-71 become a four-step "add a theme" recipe (1 XAML file with the
same keys, 1 `ThemeCatalog` arm, 1 `AppThemeOptions` entry + preview brushes, parity test
automatic). `README.md:46` becomes "shared Dark/Light/Blue themes".

## Spec delta reasoning

- `application-theme`: *Theme mode preference* (Blue offered/stored; original scenario names kept),
  *Theme palette resolution* (registry wording, new "Blue theme" and "Shared palette keys"
  scenarios, System-never-blue), *Accent color preference* (default accent follows the active
  palette's baked accent; scenario names kept). *Theme applies on next start* and *Accent applies
  live* are unchanged and absent from the delta.
- `preferences-ui`: *Application theme selector* only (four cards). All three scenario names kept.
- `launch-window-theme`: *Dark launch window theme* body updated for palette-registered rendering +
  a new "Blue surfaces" scenario; existing scenario names kept.
- `settings-theme` needs no delta: its palette wording ("the dark palette by default, the light
  palette when the effective application theme is light") stays truthful under Blue — the blue
  palette is the application's shared palette, and the requirement does not enumerate the full
  theme space.

## Deviations from the brief

- The brief suggested the registry might be a "small struct with Id/Name/Description/PaletteUri";
  D1 keeps names/descriptions in `SettingsViewModel.AppThemeOptions` and the registry maps theme →
  path only, matching the fixed decision that adding a theme is 1 registry entry + 1 settings
  option (two single-purpose edits instead of one duplicate-carrying record).
- The delta set includes the `Accent color preference` "Default accent" scenario adjustment, which
  the brief did not list: without it, the spec would claim "no accent stored → Sky" globally, which
  becomes false under the Blue theme's baked accent (D2).

## Open questions

- **Q1 — Blue color values.** The D2 table is derived, not user-approved. Exact hexes may be tuned
  during implementation; the invariants to keep are: same 28 keys, blue-shifted surfaces, AA text
  contrast (≥4.5:1 normal, ≥3:1 accent/UI components on `ChromeBgBrush`), baked
  `AccentBrush`/`AccentSelectionBrush` in the Blue `#3B82F6` family.
- **Q2 — Option wording.** "Blue Theme" / "Blue-tinted dark chrome with a baked-in blue accent" is
  proposed; the description may be shortened ("Blue-tinted dark chrome") if the card reads crowded.
- **Q3 — Accent default semantics.** Confirm the intended reading: under Blue with a never-changed
  accent, the accent is blue (not Sky). The alternative (always override with the stored default
  Sky, making the baked accent invisible) contradicts the user's "baked-in accent" decision and is
  not planned.

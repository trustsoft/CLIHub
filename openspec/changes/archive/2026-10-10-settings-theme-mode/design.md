## Context

After `theme-palette-dynamic-refactor`, palette brushes resolve at runtime via `DynamicResource`. `App.xaml` merges `LaunchTheme.xaml` (the dark palette) into `Application.Resources`; every window and style resolves palette keys through the application resource chain. `ApplicationHost.ConfigureLogging` already reads a preference before the DI container via `PreferenceReader`, which is the pattern to follow for theme resolution.

See `proposal.md` for motivation and the specs deltas for required behavior.

## Goals / Non-Goals

**Goals:**
- Add an `AppTheme` preference (Dark/Light/System; default Dark) and a light palette.
- Resolve the effective theme before any window loads and apply the matching palette.
- Surface the selector on the Appearance page; apply on next start.

**Non-Goals:**
- Accent color; window opacity/blur; compact density; live switching.

## Decisions

### A light palette with identical keys
Add `LightTheme.xaml` defining the same 26 brush keys as `LaunchTheme.xaml` with light values (light surfaces, dark text, adjusted strokes/overlays). Because references are `DynamicResource`, no style or view changes are needed to render either palette.

### Apply the palette by overriding at the application level
`ApplicationHost` resolves the effective theme (`Dark`/`Light`, with `System` read from the Windows `AppsUseLightTheme` setting) and, when light, appends `LightTheme.xaml` to `Application.Current.Resources.MergedDictionaries` after the base merge, so the light values override the dark ones for `DynamicResource` lookups. The base dark merge stays in `App.xaml`. Alternative considered: swapping the dictionary described by each style file — rejected; `DynamicResource` resolves through the application chain, so a single application-level override is sufficient and minimal.

### Preferences, resolved before the container
`AppPreferences.AppTheme` is stored as a token (`dark`/`light`/`system`) with a default-`Dark` parser; the error-free default is also applied for unknown tokens. `PreferenceReader` gains `ReadAppTheme`, and `ApplicationHost` resolves the effective theme before windows load, mirroring the log-level pattern. No schema migration.

### System detection
A small Core helper reads `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` (`1` = light); any failure falls back to dark.

### Appearance page selector
`SettingsAppearancePage` gains an "Application theme" segmented selector bound to the view model, with a hint that the theme applies after a restart.

## Risks / Trade-offs

- [Light palette quality] → derive values from the dark palette's roles; review on screen.
- [Missing a key in the light palette] → a parity check compares the key sets of both palettes.
- [Registry read failure] → fall back to dark.

## Migration Plan

No schema change. The theme defaults to dark when absent.

## Open Questions

- Exact light palette colour values — settle during implementation and review on screen; does not change the specs or task breakdown.

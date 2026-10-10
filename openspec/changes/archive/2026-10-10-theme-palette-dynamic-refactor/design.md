## Context

`LaunchTheme.xaml` defines the palette brushes (surfaces, strokes, text, accent, status). `Controls.xaml` merges it and, like `LaunchWindowStyles.xaml`, `SettingsStyles.xaml`, and `WhatsNewStyles.xaml`, references those brushes with `StaticResource`. Because a `StaticResource` is resolved when its dictionary loads, the brush values are baked into the styles; the application is therefore fixed to the dark palette. An audit found ~361 `StaticResource` references across the theme and view XAML.

See `proposal.md` for motivation. This change has no spec delta (pure refactor).

## Goals / Non-Goals

**Goals:**
- Make palette brushes resolve at runtime via `DynamicResource`, with zero visual change.
- Keep structural keys (sizing/fonts/corner radii/layout) on `StaticResource`.

**Non-Goals:**
- Any new palette, preference, accent, or opacity behaviour.

## Decisions

### Convert only palette keys
Build the set of keys defined in `LaunchTheme.xaml` (for example `ChromeBgBrush`, `PopoverBgBrush`, `TextPrimaryBrush`, `TextMutedBrush`, `AccentBrush`, `DangerBrush`, `HairlineBrush`, `FieldStrokeBrush`, and the rest of the palette) and convert every `{StaticResource <paletteKey>}` to `{DynamicResource <paletteKey>}` in the theme dictionaries and views. Non-palette keys (`Sizing.*`, `Sizing.FontFamily.*`, `Sizing.CornerRadius.*`) are left as `StaticResource`. Alternative considered: hand-editing each file — rejected; a key-driven replacement is complete and reviewable, and the build confirms no unknown keys were introduced.

### Do not restructure the merges yet
This change keeps the current palette merges intact (dark only). The theme-mode change will introduce the light palette and the startup selection; the accent change will make `AccentBrush` a swapped resource. This change only makes the references runtime-resolved so those become possible.

### Verification is visual and by key parity
The build must pass, all tests must pass, and the windows must render identically. A quick check confirms no `{StaticResource <paletteKey>}` remains and no non-palette key was converted.

## Risks / Trade-offs

- [Missing a reference] → the conversion is key-driven and greppable; a leftover `StaticResource` for a palette key would show as an unresolved-resource failure when the palette changes later, so the audit is repeated.
- [Accidental conversion of a structural key] → the key set is derived from `LaunchTheme.xaml` only.
- [Runtime lookup cost] → negligible for this number of references.

## Migration Plan

No migration. Reverting is reverting the XAML edits.

## Open Questions

- None affecting the approach or task breakdown.

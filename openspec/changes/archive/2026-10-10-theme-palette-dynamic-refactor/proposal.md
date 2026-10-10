## Why

The palette is referenced with `StaticResource`, which resolves at dictionary load time and bakes brush values into the style setters. As a result the theme cannot be changed at runtime or at startup: a Light palette (and a user-selectable accent) is impossible until these references resolve at runtime. This change is a pure refactor that enables the theme work.

## What Changes

- Convert palette brush references from `StaticResource` to `DynamicResource` across the theme dictionaries and views, so the active palette is resolved at runtime.
- Only palette brush keys (the keys defined in `LaunchTheme.xaml`) are converted. Structural keys (sizing, fonts, corner radii, layout) keep `StaticResource`.
- No visual or behavioural change: the current dark palette remains the only palette; every window looks identical before and after.

## Capabilities

### New Capabilities
<!-- None. Pure refactor: no spec-level behaviour changes. -->

### Modified Capabilities
<!-- None. -->

## Impact

- `src/CLIHub/Themes/*.xaml` (LaunchTheme, Controls, LaunchWindowStyles, SettingsStyles, WhatsNewStyles) and `src/CLIHub/Views/*.xaml` and `App.xaml` for palette-key references.
- No C# or behaviour change; no `config.json` change.

## Non-goals

- Adding a Light palette, a theme preference, accent color, or window opacity/blur (separate changes).

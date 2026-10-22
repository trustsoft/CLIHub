# Accent is application-scoped and independent of the theme

**Note:** Renamed from `accent-default-follows-palette` during planning: the user redefined the scope from
"the default accent follows the palette" to "the accent is independent of the theme", and the directory
name now states the durable model.

## Why

The current model ties the accent to theme palettes: each palette bakes in its own default accent (Sky on Dark/Light, Blue on Blue), and the startup path skips applying the default accent, relying on those baked values. This creates an asymmetric contract where the accent and theme are coupled at the palette layer but independent in the config schema.

The user's decision establishes the correct model: the accent is application-scoped with a single default value (Sky `#4CC2FF`), independent of the active theme. Theme palettes define chrome and surface brushes only; they carry no accent keys. Both startup and settings-save paths always apply the stored accent, with no default-skip branch. A theme switch never changes the accent; an accent change never changes the theme.

This change implements that durable model.

## What Changes

**Behavior:**
- The accent color is application-scoped with a single default value, Sky `#4CC2FF`, regardless of which theme is active.
- Theme palettes (`DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml`) no longer declare `AccentBrush` or `AccentSelectionBrush` keys; those keys are now runtime-provided at the application level only.
- Both the startup path (`App.ApplyAccent`) and the settings-save path (`WpfAccentPreferenceTarget.SetAccent`) unconditionally write the stored (normalized) accent into `Application.Resources`. The early-return branch when `accent == AccentPalette.DefaultHex` is removed.
- The `SelectionTint` alpha unifies to `#1F` (the function's existing constant). This is a small visual change for Dark and Blue palettes, which currently bake `#1A` alpha.
- The Blue theme picker card description changes from "Blue-tinted dark chrome with a baked-in blue accent" to "Blue-tinted dark chrome" or similar, removing the false baked-accent claim.

**Code:**
- `AccentBrush` and `AccentSelectionBrush` keys are removed from all three palette XAML files.
- `App.ApplyAccent` and `WpfAccentPreferenceTarget.SetAccent` share one implementation (via a testable seam following the `PaletteSwitcher` pattern) and always apply the accent.
- Tests add `AccentBrush` and `AccentSelectionBrush` to the runtime-provided key set and guard that no palette redeclares them.
- The `preferences-ui` theme-selector requirement is restated with the real card descriptions (`Fluent Dark (default)`, `Blue-tinted dark chrome`) and the two scenarios left stale by `live-theme-switch` are corrected, with scenario names preserved.

## Impact

**User-visible:**
- The Blue theme no longer has a baked-in blue accent; it renders with the stored accent (default Sky) like every other theme.
- Selection tint alpha on Dark and Blue palettes changes from `#1A` to `#1F`, a small increase in selection visibility.
- The Blue theme card description is updated to remove the baked-accent claim.

**Internal:**
- The accent applier logic is simplified: no default-skip branch, one shared implementation, and the accent and theme become orthogonal axes with no shared state.
- The apply order (accent then theme) stops mattering, which is a useful property for rollback and future refactoring.

**Tests:**
- Existing palette parity tests pass unchanged (all palettes still define the same key set, now with two fewer keys).
- Runtime-provided-keys and StaticResource ban guards are extended to cover the accent keys.
- A testable accent-applier seam is introduced, following the established `PaletteSwitcher` test pattern.

## Non-goals

- Per-palette default accents: the model is a single application-level default.
- An "auto/follow palette" accent mode: the accent is always explicit.
- Keeping baked accents as dead fallbacks: the palettes declare chrome only.
- Preserving the original `#1A` selection alpha on Dark/Blue: the unification to `#1F` is accepted as part of the application-scoped model.

## Dependencies

None. This change is self-contained within the theming subsystem.

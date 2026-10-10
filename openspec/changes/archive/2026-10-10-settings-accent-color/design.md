## Context

After the theme work, `AccentBrush` and `AccentSelectionBrush` are `DynamicResource`s resolved from the application palette, so a live override at `Application.Resources` reaches every window. The Settings save path already applies system preferences through `PreferenceApplier` and persists through `SettingsApplicationService`; the startup path (`App.xaml.cs`) resolves the theme before windows load.

See `proposal.md` for motivation and the specs deltas for required behavior.

## Goals / Non-Goals

**Goals:**
- Add an `AccentColor` preference (presets + custom `#RRGGBB`) with live application and startup application.
- Default to the Sky accent; reject invalid custom values.

**Non-Goals:**
- Per-window accents; recolouring other palette roles; opacity/blur.

## Decisions

### Accent value type with presets and normalization
A Core `AccentPalette` exposes the presets (Sky `#4CC2FF` default, Windows Blue `#3B82F6`, Emerald `#10B981`, Purple `#8B5CF6`, Amber `#F59E0B`) and `TryNormalize(string)` accepting `#RRGGBB` or `#AARRGGBB` and returning an `#AARRGGBB` string. The preference stores the normalized string; missing or unparsable values fall back to Sky. No schema migration.

### Live override through a port
Add `IAccentPreferenceTarget { void SetAccent(string argbHex); }`, implemented by a WPF adapter in `Views/` that sets `Application.Current.Resources["AccentBrush"]` to the accent and `["AccentSelectionBrush"]` to the accent at ~`0x1F` alpha. `PreferenceApplier` gains `ApplyAccent`, and `SettingsApplicationService.Save` applies it before persisting, rolling the previous accent back on failure (mirroring the runtime rollback). The adapter's WPF reference stays in a WPF-allowed location.

### Startup application
`App.xaml.cs` reads the stored accent (via `PreferenceReader`) and, when it differs from the default, overrides the accent brushes before any window renders — the same place the theme is resolved. This keeps a persisted accent applied on later starts even though live changes need no restart.

### Appearance page
`SettingsAppearancePage` gains the accent selector (preset swatches) and a custom hex field; the value round-trips through `SettingsInput`/`SettingsDraft`. Invalid custom input is rejected by the settings validation before any application.

## Risks / Trade-offs

- [Invalid accent breaking the UI] → normalization + default fallback; the adapter ignores an invalid value.
- [Selection tint] → derived from the accent with a fixed low alpha so it reads on both palettes.
- [Partial application then failure] → apply-then-persist with rollback, as for the other system preferences.

## Migration Plan

No schema change. The accent defaults to Sky when absent.

## Open Questions

- Exact selection-tint alpha — settle during implementation and review on screen; does not change the specs or task breakdown.

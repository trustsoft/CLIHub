# Proposal: provenance-mark-contrast

## Why

The provenance mark (`M` for manual, `D` for discovered) appears on both the Settings Projects page and the launch window project list. The style is currently duplicated between `SettingsProjectRegistryPage.xaml` and `LaunchWindowStyles.xaml`. Additionally, manual smoke testing in the Light theme revealed that the mark for manual projects is nearly invisible: the style uses `AccentSelectionBrush` (a translucent accent color) for manual projects, which loses contrast on light surfaces. Discovered projects use `TextMutedBrush` and remain readable.

This change fixes the contrast issue and removes the duplication by promoting a single `ProjectProvenanceMark` style to `Controls.xaml`.

## What changes

### Visual change: contrast-safe brush rule

- Manual projects (`CanRemove=True`): use `TextPrimaryBrush` (strong, high contrast in all themes)
- Discovered projects: keep `TextMutedBrush` (muted but readable)
- Remove the `AccentSelectionBrush` use entirely for provenance marks

The mark's character (`M` vs `D`) remains the primary distinction; the stronger foreground for manual projects is secondary emphasis.

### Structural change: promote to shared dictionary

- Add one keyed `ProjectProvenanceMark` style to `src/CLIHub/Themes/Controls.xaml` with the new brush rule
- Delete the local definition from `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` (lines 10-24)
- Delete the duplicate from `src/CLIHub/Themes/LaunchWindowStyles.xaml` (lines 254-266)
- Keep the `Style="{StaticResource ProjectProvenanceMark}"` references in both surfaces unchanged

Both surfaces already merge `Controls.xaml` transitively:
- Settings pages: app-level `SettingsStyles.xaml` merges `Controls.xaml`
- Launch window: `LaunchWindowStyles.xaml` merges `Controls.xaml`

So the shared style resolves for both without extra merge declarations.

### Backlog update

Mark the backlog item at `docs/backlog.md:243-252` ("Provenance-mark style is duplicated...") as `[done]` with a reference to this change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a visual/style change with no durable behavioral capability change; the mark's binding, data context, and rendered character are unchanged. Therefore `skip_specs: true`.

## Impact

**Modified code:**
- `src/CLIHub/Themes/Controls.xaml` — add the shared `ProjectProvenanceMark` style
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml` — remove the local style definition (lines 10-24)
- `src/CLIHub/Themes/LaunchWindowStyles.xaml` — remove the duplicate style definition (lines 254-266)
- `docs/backlog.md` — mark the duplication backlog item as done

**No breaking changes.** The style key and its binding shape are unchanged; the runtime behavior is identical except for the improved contrast.

## Non-goals

- Unit-testing XAML style resolution at runtime (WPF resource resolution only fails at runtime; no build-time gate catches a missing key)
- Changing the mark characters or their data binding
- Changing the mark's positioning or alignment in either surface

## Dependencies

None.

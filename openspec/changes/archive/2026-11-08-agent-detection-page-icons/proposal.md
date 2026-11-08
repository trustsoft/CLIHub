# Proposal: Agent Detection page icon set

## Why

The Agent Detection page currently uses the `Diagnostic` icon (E9D9, a framed pulse trace) in five
places — both setting-row card icons and all three page-header summary cards — plus `Process` (E9F3)
for the page/sidebar entry. This produces no visual differentiation across the page: every icon is
the same glyph, and the pulse-trace frame does not read as "caching" or as a time limit. The user
approved a new icon mapping (Variant "A — Радар") that assigns a distinct, semantically accurate
glyph to each role.

## What Changes

- **`SettingsNavigation.cs` — page/sidebar entry** (`agent-detection`, line 82): `IconGlyphs.Process`
  (E9F3) → `IconGlyphs.Radar` (EB44). Both the 13 px sidebar item and the 20 px page-header emblem
  derive from this one value via `SelectedPage.Icon`; no XAML change is needed.
- **`SettingsAgentDetectionPage.xaml` — Cache TTL row icon** (line 38): `IconGlyphs.Diagnostic`
  (E9D9) → `IconGlyphs.Recent` (E823).
- **`SettingsAgentDetectionPage.xaml` — Version check timeout row icon** (line 49):
  `IconGlyphs.Diagnostic` (E9D9) → `IconGlyphs.Stopwatch` (E916).
- **`SettingsWindow.xaml` — CACHE TTL summary card icon** (line 238): `IconGlyphs.Diagnostic`
  (E9D9) → `IconGlyphs.Recent` (E823).
- **`SettingsWindow.xaml` — TIMEOUT summary card icon** (line 243): `IconGlyphs.Diagnostic`
  (E9D9) → `IconGlyphs.Stopwatch` (E916).
- **`SettingsWindow.xaml` — DEFAULTS summary card icon** (line 249): `IconGlyphs.Diagnostic`
  (E9D9) → `IconGlyphs.UpdateRestore` (E777).
- **`IconGlyphs.cs` — four new constants**: `Radar = "\uEB44"`, `Recent = "\uE823"`,
  `Stopwatch = "\uE916"`, `UpdateRestore = "\uE777"`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme`: The "Agent Detection page summary cards" requirement currently states "All three
  cards SHALL use the Diagnostic icon" and its scenarios pin the Diagnostic icon by name. The
  requirement is updated to specify the per-card icon mapping (CACHE TTL → Recent, TIMEOUT →
  Stopwatch, DEFAULTS → UpdateRestore), including the invariant that a setting row and its
  corresponding summary card share one glyph. The "Sidebar navigation and page styling" requirement
  is also updated to reflect the Radar glyph for the Agent Detection sidebar item and page-header
  emblem (currently Process).
- `preferences-ui`: The "Agent Detection page content" requirement pins the sidebar icon
  (`IconGlyphs.Process`) and the DEFAULTS card icon (`Diagnostic`). It is updated to name Radar and
  UpdateRestore so the spec set stays internally consistent after archiving.

## Impact

- **Source files changed**:
  - `src/CLIHub/Theming/IconGlyphs.cs` — add four new glyph constants.
  - `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:82` — swap `Process` → `Radar`.
  - `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:38,49` — swap two row icons.
  - `src/CLIHub/Views/SettingsWindow.xaml:238,243,249` — swap three summary card icons.
- **Spec**: `settings-theme` — "Agent Detection page summary cards" requirement and its scenarios
  updated; sidebar navigation requirement updated for the Agent Detection emblem glyph.
- **Behavior**: none. All changes are purely presentational (Segoe MDL2 glyphs); no logic,
  configuration schema, persistence, or navigation changes.
- **Tests**: none need updating; no test asserts specific icon glyph values for this page.

## Explicit non-goals

- Changing any icon on any other Settings page.
- Changing the XAML icon-binding mechanism or the sidebar/header layout.
- Adding configuration for icon themes or glyph customization.
- Changing page content, behavior, or configuration schema.
- Updating `docs/architecture/ui.md`; that document describes the layout mechanism, not specific
  glyph assignments, and does not enumerate the Agent Detection icons by name.

## Dependencies

None.

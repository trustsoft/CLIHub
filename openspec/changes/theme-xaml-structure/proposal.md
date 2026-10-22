# Theme XAML Structure

## Why

The theme and Settings XAML layer resists extension: adding a theme costs seven edits across five files,
`SettingsWindow.xaml` carries roughly 600 lines of near-identical card markup for nine summary cards and
about 200 lines of per-page visibility triggers plus header-template trigger chains, and the application
merge tree loaded `Controls.xaml` three times. A new Settings page means touching metadata and several
trigger chains; a tenth summary card means copy-pasting a fifty-line block.

These are structural defects only — nothing user-visible changes in this work.

## What Changes

Six stages, each ending build + tests + `openspec validate --all` green and a user acceptance checkpoint:

1. **Theme descriptor** — one WPF-layer source of truth for per-theme data (name, description, palette
   path, preview keys); the Appearance page renders from bindings instead of three `DataTrigger` sets.
2. **Theme picker extraction** — picker resources move to their own dictionary; `App.xaml` drops from
   five merges to three and `Controls.xaml` is loaded once for the whole application.
3. **`SummaryCard`** — one control replaces the nine card blocks, including the icon/value tone
   vocabulary and the conditional danger-toned folder-health line.
4. **Settings page host** — page metadata names the header/summary templates, and three small converters
   replace the ten per-page visibility styles and the three header trigger chains.
5. **Sizing token sweep** — tokenize only values that repeat or encode a role, reorganize `Sizing.xaml`
   by category, and replace the two look-based token names (`Chunky`, `Spacing`).
6. **Guards and documentation** — three static guards (colour literals outside palettes, `StaticResource`
   on a palette key, duplicate application-level keys), corrected comments, the add-a-page recipe and the
   theme data locations, plus the deferred items in `docs/backlog.md`.

Stages 1-2 are implemented, verified green and accepted. Stage 3-6 are described in `tasks.md`.

## Impact

- **Users**: none. No visual, behavioural or performance change is intended, and each stage is accepted
  against a manual comparison.
- **Theme contributors**: adding a theme is one palette file plus one descriptor entry.
- **Settings authors**: a summary card is a control instantiation; a page is one metadata entry, one
  template key set, and one page element.
- **Maintainers**: the three new guards turn three classes of silent regression into failing tests.
- **Code**: roughly 430 XAML lines removed, replaced by a control, a metadata record and tests.

## Non-Goals

- Changing palette colours or adding a theme.
- Deleting or refactoring `src/CLIHub/Views/MainWindow.xaml` (dead legacy window, deliberately parked in
  `docs/backlog.md`).
- Runtime theme switching (separate change: `live-theme-switch`).
- Tokenizing every numeric literal; only values that repeat or name a role become tokens.
- A single registration point for Settings pages (the metadata record and the XAML page host remain two
  places; parked in `docs/backlog.md` with a revisit trigger).
- Re-doing the archived `2026-10-22-tidy-theme-resources` scope.

## Dependencies

None. The change is self-contained; it does not depend on or block `live-theme-switch`.

## Open Questions

None blocking. The remaining design risks are recorded per stage in `design.md`; the two deferred
improvements (single page registration point, header-slot keys resolved at runtime) are in
`docs/backlog.md` with their revisit triggers.

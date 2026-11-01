# Proposal

## Why

The Settings window pins the active page's title and one-line description above the scrolling
content (`src/CLIHub/Views/SettingsWindow.xaml:405-479`). Pages that have no summary row and no
header action — General & Startup, Hotkeys & Quick Launcher, Terminal Profiles, Appearance, Logs —
leave the entire left side of that header empty; the title has nothing anchoring it to a section
identity. The sidebar already carries a per-page glyph (`SettingsPage.Icon`,
`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:26-34,41-110`) that the header does not reuse.

The user reviewed four mockup variants in `ui/mockups/settings-page-emblem.html` (A: 40×40 neutral
tile, B: bare glyph, C: accent tile, D: unchanged) and chose **B — a bare glyph, no tile, no accent,
no interaction** — a static orientation cue that reuses the existing icon with no new visual weight.

## What Changes

- Add a static emblem to the Settings page header, to the left of the title/description block: the
  active page's existing sidebar glyph (`SettingsPage.Icon`), rendered at 20 DIP in the icon font
  (`Sizing.FontFamily.Icon`), in `TextMutedBrush`, with a 12 DIP gap before the title text.
- The emblem sits in its own grid column, outside the title `WrapPanel`, so the Updates page's title
  chip (`HeaderTitleExtraKey="UpdatesHeaderTitleExtra"`) keeps wrapping with the title exactly as
  today and never separates from it.
- The emblem is vertically centered against the full title+description block and does not change the
  header's height (confirmed in the mockup: the title+description block measures ~40.4 DIP at all
  four variants, and a 20 DIP glyph fits inside it with room to spare).
- The emblem is decorative and non-interactive: it never takes keyboard focus and exposes no action
  or tooltip. WPF has no per-element mechanism to remove it from the accessibility tree, so it is
  announced as a text element the same way every other icon glyph in the window already is; this
  change does not promise an exclusion WPF cannot deliver, and the residual gap goes to
  `docs/backlog.md` for an eventual app-wide fix.
- Applies uniformly to every Settings page; no page opts out, and no new `SettingsPage` field is
  added — `Icon` already exists and is reused as-is.
- Mark `ui/mockups/settings-page-emblem.html` as delivered with variant B chosen, in
  `ui/mockups/README.md`.

## Capabilities

### Modified Capabilities

- `settings-theme`: the "Persistent page header" requirement gains the page emblem as part of the
  fixed header; new scenarios cover the emblem's presence, source, and non-interactive behavior, and
  an existing scenario is extended to confirm header height is unaffected.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml` — page-header `Grid` (currently rows: title block / summary
  row / hairline; columns: `*` / `Auto`) gains a new `Auto` column before the title column, with the
  emblem `TextBlock` placed in it.
- `src/CLIHub/Themes/Sizing.xaml` — gains a new token, `Sizing.FontSize.GlyphLarge` (20), for the
  emblem's font size; `design.md` Decision 4 records why this value is tokenized while the 12 DIP
  gap to the title stays a literal.
- `docs/architecture/ui.md` — "Settings Page Layout" section gains one or two sentences documenting
  the emblem.
- `ui/mockups/README.md` — status line and `settings-page-emblem.html` section updated from "open
  design comparison, no variant chosen" to "delivered, variant B".
- `CHANGELOG.md` — gains an `### Changed` bullet under `## Unreleased` describing the emblem,
  following the project's per-change CHANGELOG convention (confirmed in archived changes such as
  `openspec/changes/archive/2026-11-01-agent-row-inset-alignment`).
- `docs/backlog.md` ("UI and theming" section) — gains an entry recording that the emblem, like the
  window's other decorative icon glyphs, is exposed to assistive technology as a plain text element
  because WPF has no per-element accessibility-view switch; the entry names the `DecorativeTextBlock`
  remedy and its app-wide scope as the trigger for a future change.
- No change to `SettingsPage`, `SettingsPageCatalog`, `SettingsNavigationViewModel`, or any ViewModel:
  the emblem binds directly to the already-exposed `SelectedPage.Icon`.

## Non-Goals

- No per-page opt-out and no new `SettingsPage` metadata field — `Icon` is reused unchanged.
- No background tile, no accent color, no border, no hover/pressed state, no click behavior (variants
  A, C, and any interactive affordance are explicitly rejected).
- No change to the summary row, header action column, or hairline divider layout besides the new
  leading column.
- No rework of the sidebar glyph list, `IconGlyphs`, or the icon font itself.
- No update to `docs/images/settings-*.png` — release preparation owns screenshot refresh
  (`docs/releasing.md`, step 0), not this change.

## Dependencies

- None on other in-flight OpenSpec changes. Depends only on the already-delivered `preferences-ui`
  Settings window and the page-header layout documented in `settings-theme`.

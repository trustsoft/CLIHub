## Context

See `proposal.md` — Why. `SettingsViewModel` defines the navigation catalog as a `SettingsPage` record (`Key`, `Title`, `Group`, `IsImplemented`) in `AllPages`; `SettingsWindow.xaml` renders it as a grouped `ListBox` (`SidebarList`/`SidebarItem` styles) with a text-only item template. `IconGlyphs` holds shared MDL2 constants referenced from XAML via `{x:Static themes:IconGlyphs.Name}`. The hotkeys page header text lives in `SettingsHotkeysPage.xaml` and the label appears in the `preferences-ui` spec.

## Goals / Non-Goals

**Goals:**

- A leading icon per page item, sourced from `IconGlyphs`, without changing navigation behavior.
- Rename the hotkeys page to "Hotkeys & Quick Launcher" in the sidebar and its page header.

**Non-Goals:**

- Trailing badges, search, profile card, other renames.

## Decisions

### D1 — Extend `SettingsPage` with an icon key

Add an `Icon` field to the `SettingsPage` record and supply a glyph per page in `AllPages`. **Why:** keeps the catalog as the single source for both title and icon, so the sidebar template stays a simple binding. **Alternative rejected:** a `Key → icon` dictionary in the view — duplicates the catalog and drifts.

### D2 — Render the icon in the sidebar item template with a fixed icon column

Update the `SidebarItem`/list template to show the glyph in a fixed-width column before the title (reusing the launch window's icon typography). **Why:** a fixed column keeps titles aligned across items regardless of glyph width. **Alternative rejected:** inline glyph sizing — misaligns titles.

### D3 — Icons from the existing shared glyph set, extended as needed

Add the missing MDL2 constants (for example keyboard, robot, terminal, palette, monitoring) to `IconGlyphs` and map each page to one. **Why:** consistent with the launch window's icon usage and compile-time checked. **Alternative rejected:** image assets — unnecessary weight for glyphs.

### D4 — Rename only the hotkeys page

Change the page title to "Hotkeys & Quick Launcher" in `AllPages` and in `SettingsHotkeysPage.xaml`'s page header. **Why:** the target uses that label; the user chose to rename only this page. **Alternative rejected:** renaming other pages — explicitly deferred per-name.

## Risks / Trade-offs

- **Glyph availability** → choose well-known MDL2 codes and visually confirm in smoke; adjust codes if a glyph is missing.
- **Tests referencing the old label** → update the navigation/spec references; the label is not asserted by key-based tests.

## Migration Plan

- None; presentational and label-only.

## Open Questions

- None.

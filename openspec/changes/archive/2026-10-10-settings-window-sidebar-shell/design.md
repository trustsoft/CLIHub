## Context

The current Settings window is a single `ScrollViewer` containing one `StackPanel` of sections (`src/CLIHub/Views/SettingsWindow.xaml`). `SettingsViewModel` owns the whole settings draft, validation, and the Save/Cancel/Check-for-updates commands; controls bind directly to its properties. Theming lives in `src/CLIHub/Themes/SettingsStyles.xaml` and the shared launch-window styles.

See `proposal.md` — Why for motivation and the specs deltas for the required behavior. This change is a UI restructure: the draft model, validation, persistence, and system-application logic stay as they are.

## Goals / Non-Goals

**Goals:**
- Introduce a sidebar-navigated shell with three groups and eight pages.
- Re-home existing controls to their pages without changing preference behavior.
- Give later changes (roadmap Phase 1/2) a stable place to add a page's settings.
- Reuse the existing single `SettingsViewModel` as the draft owner.

**Non-Goals:**
- Adding, removing, or renaming any preference; changing `config.json`; touching `CLIHub.Core`.
- Per-page view models, search, or section deep-linking (deferred).
- Any of the ❌/⏳ items from the roadmap.

## Decisions

### Navigation: `ListBox` + `ContentControl` over a `TabControl`
Use a `ListBox` for the sidebar (bound to a static page catalog) and a `ContentControl` for the active page, selecting a page view via `DataTemplate`/`DataTrigger` on the selected page key. Alternatives: a restyled `TabControl` (`TabStripPlacement=Left`) is less markup but fights the drawn chrome and active-highlight styling; a custom `ItemsControl` with manual selection duplicates `ListBox` behavior. `ListBox` gives selection state and keyboard navigation for free and keeps styling in `SettingsStyles.xaml`.

### Page content as `UserControl`s, not inline markup
Extract each page into a `UserControl` (`SettingsGeneralPage`, `SettingsHotkeysPage`, …) so the window shell stays small and later changes touch one page file. Alternatives: keep all pages inline (window XAML grows without bound); `DataTemplate`s in one dictionary (better than inline but still one large file). Per-page `UserControl`s match the project's existing view decomposition.

### One `SettingsViewModel` as the draft owner (for now)
Pages bind to the existing `SettingsViewModel`; navigation state (selected page) is added to it. Alternatives: a `SettingsShellViewModel` owning child page view models. Deferred — Phase 0 has no page-specific logic, and extracting page VMs now would move properties with no behavioral gain. Revisit when Phase 1/2 pages add logic.

### Page catalog as static data
Define groups/pages as a static catalog (group label, page key/title, whether implemented). Unimplemented pages resolve to a shared empty-state view. This keeps "add a page" a data change and makes the empty state explicit rather than accidental missing markup.

### Window sizing
Widen the default window (sidebar + content, e.g. ~720×560) and raise `MinWidth`; keep `ResizeMode`, drawn chrome, Escape-to-close, and the footer Save/Cancel unchanged.

### Re-homing mapping
Startup toggles → General & Startup; global hotkey → Hotkeys & Launchers; default runtime → Terminal Profiles; agents probe → CLI Agents; path display → Appearance; update check → Updates. Only XAML moves; bindings and commands are unchanged.

## Risks / Trade-offs

- [Large window XAML becomes unmaintainable] → one `UserControl` per page; shell holds only chrome, sidebar, and host.
- [Styling drift from the launch window] → add sidebar styles to the shared `SettingsStyles.xaml` reusing existing palette brushes; no new colors.
- [Existing Settings tests assert on the flat layout] → keep `SettingsViewModel` API unchanged; add navigation tests; adjust only view-level expectations.
- [Wider default window surprises users] → keep it resizable and centered; larger `MinWidth` is intentional to fit the sidebar.

## Migration Plan

No data migration: `config.json` and `CLIHub.Core` are untouched. Purely additive UI. Rollback is reverting the view/theme changes; persisted preferences are unaffected.

## Open Questions

- Exact default window dimensions and sidebar width — settle during implementation against the mockup; does not change the specs or task breakdown.

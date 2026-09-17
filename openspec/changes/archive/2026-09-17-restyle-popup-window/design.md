# Design — Restyle Popup Window

## Context

The popup (`src/CLIHub.App/Views/PopupWindow.xaml`) is a 720x420 light
window with two list columns, a WPF `Menu` per pane, and a footer with
brand + version chip, centered status, and three MDL2-icon buttons. The
dark visual vocabulary (palette brushes, section labels, chips, buttons)
already exists as keyed resources in `App.xaml` from the settings-window
restyle. The chosen reference is the split-view mockup
(`docs/ui/popup-split.png`, menu companion `popup-split-menu.png`); the
grid/cards variants are explicitly not chosen.

## Goals / Non-Goals

- Goals: popup visually matches the split-view mockup; pane/row anatomy
  (projects 60 px rows with 42 px thumbs, agents with stacked name/version
  and row dividers, accent selection) implemented with reusable styles.
- Non-Goals: any behavior change — no `resume`, no `Update`/`Init`
  submenus, settings footer button stays disabled; grid/cards views are
  not built.

## Decisions

- **Reuse the `App.xaml` dark palette; add only popup-specific styles.**
  Row templates (project item, agent item), pane title with `Actions`
  chevron button, and icon-button styles are new keyed resources; palette,
  chips, and labels come from the existing resources unchanged.
- **Split-view geometry from the mockup spec** (UI-project AGENTS.md):
  window 543 px wide; Projects pane 300 px with a 42 px thumb, 60 px rows,
  selection = 10 % accent background + 3 px accent bar; vertical hairline
  divider between panes; agent rows 60 px with 28 px logos and 1 px row
  dividers inset 10/12 px; footer strip with the shared 14 % black tint;
  uppercase 11 px section labels with the `Actions` button right-aligned
  (normal state chrome-less, hover shows the standard chrome).
- **Window sizing:** switch from fixed `Height=420` to `SizeToContent`
  with the panes' content driving height (capped with MaxHeight so a long
  list scrolls inside the panes instead of growing the window; the mockup
  shows ~7 rows). `PopupPositioner` reads `window.Width/Height` at
  position time, so the new size needs no algorithm change — verify
  on-screen clamping manually at 100 %/150 % DPI.
- **`Actions` button:** replace the WPF `Menu` chrome with a flat
  chevron-styled `Button` + `ContextMenu` (mockup anatomy: text +
  chevron, chrome on hover only). The menu contents (Add/Remove) and the
  light-dismiss behavior of `ContextMenu` match the current usage.
- **Icon buttons in the footer:** keep the existing MDL2 glyphs but restyle
  to 18 px `#D8D8D8` with a soft hover overlay; the disabled settings
  button uses the shared disabled style. Swapping to vector paths is not
  worth it before the icon set is finalized.
- **Selection/availability visuals:** selected project uses the mockup's
  accent treatment; the run button stays a single icon button per agent
  row (resume button is not drawn — its behavior does not exist yet and a
  permanently disabled control would contradict the mockup's availability
  language).

## Risks / Trade-offs

- [`SizeToContent` + scrolling needs an explicit cap] → Set `MaxHeight`
  on the panes' `ListBox`es (ScrollViewer inside stays default), verify
  with long project lists.
- [Row hover/selection states are mockup-only details] → Implement
  selection per mockup; keep hover subtle (same overlay as buttons) and
  note it as an extension, not a mockup deviation.
- [Dark `ContextMenu` needs its own template] → Style the popup's
  `ContextMenu` with the popover chrome from the mockup (`#2B2B31`,
  8 px radius, shadow) or the default system menu will render light.

## Migration Plan

UI-only change; no data or config migration. Rollback = revert the commit.
Manual verification: hotkey open/close, project add/remove, agent run,
footer actions, position clamping — plus visual comparison against
`docs/ui/popup-split.png`.

## Open Questions

- None.

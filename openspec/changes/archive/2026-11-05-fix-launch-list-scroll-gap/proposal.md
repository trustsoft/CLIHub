# Proposal: Fix Launch List Scroll Gap

## Why

Scrolling the launch window's Agents list to its end leaves a blank band at the bottom of the pane
instead of ending flush with the list's bottom edge. The Projects list does not show this defect, so the
two panes — which are meant to share one visual rhythm — behave differently under the same user action.

Measured on the built app (device pixels, 150% DPI; see `design.md` for the full method and numbers):
with 8 agents, scrolling the Agents list to 100% leaves an 83 px (55 DIP) blank band below the last row,
while the Projects list under the same scroll ends flush. The root cause is two independent facts about
the current styles:

1. `PaneList` (`src/CLIHub/Themes/Common/LaunchWindowStyles.xaml:170-242`) scrolls by item
   (`ScrollViewer.CanContentScroll="True"`, the WPF default for `VirtualizingPanel.VirtualizationMode`),
   so the end-of-scroll offset is quantized to a whole row.
2. `AgentRowContainer` (`LaunchWindowStyles.xaml:139-167`) draws its row divider as a `Rectangle` that is
   a sibling of the row's `Border` inside a `StackPanel`, so the divider adds 1 DIP of layout height to
   every agent row. The agent row pitch (63 DIP) does not divide `Sizing.PaneList.MaxHeight` (434 DIP,
   `Sizing.xaml:68`) evenly, so the quantized end-of-scroll offset over-shoots and leaves a remainder
   blank. The project row pitch (62 DIP) divides 434 evenly by coincidence, which is why the Projects list
   is unaffected today.

The same divider-adds-height defect also makes the Agents list show a scrollbar at exactly seven agents,
even though seven rows at the project pitch would fit the pane exactly with no scrolling — a second,
latent symptom of the same row-rhythm mismatch.

## What Changes

- `PaneList` style gains `VirtualizingPanel.ScrollUnit="Pixel"` (while keeping
  `ScrollViewer.CanContentScroll="True"` so virtualization stays on), so the scroll offset is expressed in
  pixels and the end of the scroll always fills the viewport exactly, regardless of row height arithmetic.
- `AgentRowContainer`'s row divider moves from a layout-height `StackPanel` sibling to a zero-height
  overlay drawn on top of the row (for example inside the existing `Border`/`ContentPresenter`, anchored
  to the top edge), so the divider adds no layout height. The agent row pitch becomes 62 DIP, identical
  to the project row pitch, so seven agent rows total exactly 434 DIP — the same as seven project rows.
- The divider keeps its current look and visibility rule: 1 DIP, `RowDividerBrush`, 12 DIP inset on both
  sides, shown between rows only (never above the first row), still driven by
  `IndexToDividerVisibilityConverter` (`src/CLIHub/Converters/IndexToDividerVisibilityConverter.cs`).
- The `PaneList` comment at `LaunchWindowStyles.xaml:178-183` is corrected: it currently claims the
  `MaxHeight`/row-height multiple is what makes the scrolled end position land flush, which is the
  opposite of the actual defect this change fixes. The comment is rewritten to describe the template's own
  2 px deficit (no default 1px `ListBox` border) and to no longer attribute "flush at the end" to
  row-height arithmetic, since `ScrollUnit="Pixel"` makes that property structural rather than arithmetic.

## Impact

- `openspec/specs/launch-window-layout/spec.md`: MODIFIED requirements "Scrolling lists" and "Agents pane
  and row sizing" (existing scenario names preserved; behavior extended, see `specs/launch-window-layout/spec.md`
  delta in this change).
- `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml`: `PaneList` style (scroll unit, comment),
  `AgentRowContainer` style (divider becomes an overlay).
- No change to `Sizing.xaml` token values (`Sizing.PaneList.MaxHeight` stays 434; `Sizing.AgentRow.Height`
  stays 60).
- No change to `LaunchWindow.xaml`, `ProjectRowContainer`, or any ViewModel.
- Visible effect: both lists end flush with their pane's bottom edge when scrolled to the end; a pane with
  exactly seven rows (of either kind) shows no scrollbar.

## Non-Goals

- No change to the launch window's maximum height or `Sizing.PaneList.MaxHeight` value.
- No change to pane widths or minimum widths (`Sizing.ProjectPane.MinWidth`, `Sizing.AgentPane.MinWidth`).
- No change to the project row template, `ProjectRowContainer`, or `Sizing.ProjectRow.Padding`.
- No change to list order, composition, or item data.
- No change to scrollbar appearance, the custom `ScrollViewer` template, or `SlimScrollBar.xaml`.
- No new visible scrolling hardware (no new scrollbar, no snap indicators).
- No new NuGet dependencies.
- No changes outside the launch window's styles and this change's spec text.

## Dependencies

None. This change is independent and has no blocking or dependent OpenSpec changes.

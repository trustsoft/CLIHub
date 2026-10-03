## Context

See `proposal.md` - Why. The current pane Actions menus are `Button.ContextMenu` instances in
`src/CLIHub/Views/LaunchWindow.xaml` (`ProjectsActionsButton`, `AgentsActionsButton`), styled by
`DarkContextMenu` / `DarkMenuItem` in `src/CLIHub/Themes/LaunchWindowStyles.xaml`. Because a
`ContextMenu` is rendered in a separate tree, the styles are keyed, the per-item markup is
duplicated, and `OpenMenuAlignedRight` in `LaunchWindow.xaml.cs` right-aligns the menu by
measuring it after it opens. All commands already exist on `LaunchWindowViewModel`; there is no
`CLIHub.Core` involvement.

## Goals / Non-Goals

**Goals:**
- Present both pane Actions menus as in-tree popovers driven by per-pane action collections.
- Give every entry a glyph, with hover, checked, and disabled states.
- Remove the `ContextMenu` styles and the `OpenMenuAlignedRight` code-behind.

**Non-Goals:**
- Changing which actions exist, their commands, or their enabled logic.
- Converting the footer controls or other popups.
- Introducing submenus or multi-level menus.
- Touching `CLIHub.Core`, services, or persisted configuration shape.

## Decisions

### Container: an in-tree `Popup`, not a `Menu` or `ContextMenu`

Use a `Popup` declared inside `LaunchWindow.xaml`, with `PlacementTarget` set to the pane's
Actions control, `Placement="Bottom"`, `AllowsTransparency="True"`, and `StaysOpen="False"`.
Unlike `ContextMenu`, a `Popup.Child` stays in the window's logical tree, so it inherits the
window's merged resources and `DataContext`: `{StaticResource}` brushes and `{Binding
ProjectsActions}` resolve without keyed-style duplication or `PlacementTarget` plumbing.

- Alternative - keep `ContextMenu` (rejected): separate tree, keyed styles, and the
  measure-after-open alignment hack.
- Alternative - the reference's `Menu` + `MenuItem` `PART_Popup` (rejected): `Menu` is a
  horizontal command bar whose menu-bar semantics and built-in submenu machinery are unnecessary
  for a single popover button.

### The Actions control toggles the popup declaratively

Replace each `Button` with a `ToggleButton` whose `IsChecked` is bound two-way to the popup's
`IsOpen` through `ElementName`. Opens and closes (including outside-click via `StaysOpen="False"`)
need no view-model state and no code-behind.

### Data model: `MenuAction` in `CLIHub.ViewModels`

Add `MenuAction` (an `ObservableObject`) and `MenuActionTone`:

```
MenuAction: Label, Glyph, Command, IsCheckable, IsChecked, IsSeparator
```

`LaunchWindowViewModel` exposes `IReadOnlyList<MenuAction> ProjectsActions` and
`AgentsActions`, built once in the constructor from the existing commands and `IconGlyphs`.
Enabled state is not modelled: each entry's `Button.Command` disables itself via the command's
`CanExecute`. Separators are `MenuAction` entries with `IsSeparator = true`.

The availability filter is the only stateful entry: the view model keeps a reference to the
filter `MenuAction` and syncs `IsChecked` with `ShowOnlyProjectAgents` in both directions, reusing
the existing `_suppressFilterChange` guard to avoid re-entrancy.

### Rendering: one `ItemsControl` with a template-switching `ContentControl`

Each popup contains an `ItemsControl` bound to its collection. Its `ItemTemplate` is a
`ContentControl` whose `Style` swaps `ContentTemplate` by `DataTrigger`: `IsSeparator` renders a
hairline, `IsCheckable` renders a `ToggleButton` bound two-way to `IsChecked`, and the default
renders a `Button` bound to `Command`. Both entry templates share one visual structure (glyph +
label) and one button style with hover/disabled triggers, so only the toggle adds the checked
background.

The item templates live in `LaunchWindow.xaml`'s window resources, not in
`LaunchWindowStyles.xaml`: an entry wires the window's `Click` handler, and only a XAML file with a
code-behind class can resolve an event handler. The entry styles and the popover card style stay in
the styles dictionary.

### Icons: existing glyphs, one neutral color

Keep the Segoe MDL2 glyphs from `IconGlyphs.cs`, drawn in a single neutral tone
(`TextMutedBrush`). Every glyph shares the same color, so the menu reads as one list rather than a
set of differently colored actions. *Alternative:* a semantic color per action role (rejected — the
mixed colors made the menu look noisy).

### Open control merges with the menu

The open Actions control takes the menu card's background and border, with its bottom edge left
open and its top corners rounded, and the popup opens flush with the control's bottom (offset by
one pixel so the borders overlap rather than double). The card rounds its top-left and bottom
corners and squares its top-right, which sits under the control. A thin rectangle inside the popup,
as wide as the control, masks the card's top border under the control, so no seam is drawn between
them. Because the control's bottom border is removed, its bottom padding is increased by the same
pixel so the label and chevron keep their position. The menu surface uses the control's pressed
tone (`PopoverOpenBrush`), so pressing the control previews the menu's fill. The result is one panel
with the control as its header tab. *Alternative:* keep the translucent hover tone on the control
and a square top-left card corner (rejected — it did not match the card's background and left a
visible dividing line).

### Sizing and right alignment: content-sized popover, measured offset

Let the popover size to its entries (a `MinWidth` floor keeps very short menus from looking thin)
instead of a fixed width, so the Projects menu is narrower than the Agents menu. The popup is
right-aligned by measuring its content when it opens (`Popup.Opened`) and setting
`HorizontalOffset = control.ActualWidth - content.DesiredSize.Width`. *Alternative:* a fixed width
with a declarative offset (rejected — it left the shorter Projects menu mostly empty).

### Close on invoke: one shared handler

An entry's command runs on click; a single shared `Click` handler on the entry button then closes
the owning popup. Alignment and outside-click closing are declarative; the click handler and the
handlers below are the only code-behind in the flow.

### Escape closes the menu before the window

Escape closes an open menu and only hides the window on a second press. A `Popup` is a separate
window, so key events raised inside it do not bubble to the window's `KeyDown`: the window handler
closes the menu when focus is on the Actions control, and a `PreviewKeyDown` handler on the popup's
content root covers the case where focus is inside the menu.

## Risks / Trade-offs

- [Popup interaction could deactivate the launch window, which hides on deactivate] -> The popup
  is part of the window; verify that opening and clicking it does not raise the window's
  `Deactivated` handling (the existing `ContextMenu` did not).
- [Two-way `IsChecked` sync could recurse with `ShowOnlyProjectAgents`] -> Reuse
  `_suppressFilterChange` and update the mirror value only when it actually changes.
- [The popover's measured width could differ from its finally arranged width, skewing alignment]
  -> Measure the child with unbounded width in `Popup.Opened` and use its desired width; both
  menus are short, fixed strings, so the measurement is stable.
- [The open control and the card are in different trees (window vs. popup), so their shared edge
  can show a seam] -> Match background and border, round the card's top-left, square its top-right,
  overlap by one pixel (vertical offset of -1), and mask the card's top border under the control
  with a rectangle as wide as the control.

## Migration Plan

1. Add `MenuAction`, `MenuActionTone`, the tone converter, and the palette brush.
2. Add the action collections to `LaunchWindowViewModel` and wire the filter sync.
3. Add the popup card and entry styles to `LaunchWindowStyles.xaml`, the item templates to
   `LaunchWindow.xaml`, and replace the two `Button` + `ContextMenu` blocks in `LaunchWindow.xaml`.
4. Remove `DarkContextMenu`, `DarkMenuItem`, `MenuIcon`, `MenuSeparator`, `OpenMenuAlignedRight`,
   and the two open handlers.
5. Build and run the window against both panes; confirm alignment, icons, states, and close
   behavior.

Rollback is a file revert: the commands and services are untouched.

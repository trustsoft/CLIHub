## Why

The two pane Actions menus are built on the native WPF `ContextMenu`, which lives in a
separate visual tree (`PopupRoot`) and cannot inherit the window's styles, resources, or
implicitly-scoped templates. This forced keyed styles, duplicated per-item XAML in
`LaunchWindow.xaml`, and a code-behind alignment hack (`OpenMenuAlignedRight`). The reference
implementation (`DropdownMenuDemo`) demonstrates that a fully-templated `Popup` hosted in the
window's own tree, with data-driven items, removes these constraints and makes the menu easy to
restyle. Reworking our menus the same way keeps the visualization from the mockup — a rounded
popover with icon-plus-label entries — while making both panes consistent.

## What Changes

- Replace the native `ContextMenu` behind each pane's Actions control with an in-tree `Popup`
  anchored to that control.
- Introduce a small, UI-only action model (label, glyph, enabled state, checked state, command)
  and expose per-pane action collections on `LaunchWindowViewModel`.
- Render each entry from a data template: icon glyph plus label, with hover, checked, and
  disabled visual states, all glyphs in one neutral color.
- Remove `OpenMenuAlignedRight` and the `ContextMenu`-specific styles; size each menu to its
  entries and right-align it with the Actions control by measuring the open popup.
- Make the open Actions control and its menu read as one panel, sharing a background and outline.
- Close the popup when an entry is invoked or when the user clicks outside it.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `main-window-layout`: the "Actions menu presentation" requirement changes — the menu becomes a
  data-driven, themed popover whose entries carry a neutral icon and hover/checked/disabled
  states, and which closes when an action is invoked.

## Impact

- `src/CLIHub/Views/LaunchWindow.xaml` — the two Actions buttons and their `ContextMenu` markup.
- `src/CLIHub/Views/LaunchWindow.xaml.cs` — `OnOpenProjectsMenu`, `OnOpenAgentsMenu`,
  `OpenMenuAlignedRight`, and the `ContextMenu` open logic.
- `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` — new per-pane action collections.
- `src/CLIHub/ViewModels/` — new action descriptor type(s).
- `src/CLIHub/Themes/LaunchWindowStyles.xaml` — replace `DarkContextMenu`/`DarkMenuItem` with a
  popup and item template.
- No change to commands, services, or the `CLIHub.Core` project.

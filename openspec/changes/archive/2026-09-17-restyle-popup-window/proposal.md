# Restyle Popup Window

## Why

The popup is still the default light WPF look, while the chosen UI mockup
(`docs/ui/popup-split.png`, split view) defines a dark chrome-frame visual
language that the settings window already implements. Both hotkey surfaces
must read as one product.

## What Changes

- Restyle the popup window to the split-view mockup:
  - dark chrome frame (`#26262C`), window width tightened to the mockup's
    543 px;
  - two panes separated by a vertical hairline divider; the Projects pane and
    the footer share the darker 14 % black tint so they frame the content;
  - pane titles become uppercase micro-labels with the `Actions` button
    right-aligned (chevron style, hover chrome);
  - project rows: 42 px logos, name + path stacked, selected row with the
    10 % accent background and the 3 px accent bar;
  - agent rows: 28 px logos, name with the version below it (fixed-width
    meta block), subtle 1 px row dividers;
  - footer: brand `CLIHub` + version chip left, status in the center, and
    the three icon buttons (open-data-folder, settings, exit) restyled to
    the dark language.
- Reuse the shared dark style resources introduced by the settings window
  restyle (`App.xaml`): palette, section labels, chips; new popup-specific
  styles (project/agent rows, icon buttons) are added alongside them.
- No behavior change: hotkey open/close semantics, project add/remove,
  single `run` action with its availability rules, footer actions, and
  status reporting are unchanged. `resume` and the `Update`/`Init` menu
  submenus remain separate future changes.
- **Added during review**: the footer settings button becomes enabled and
  opens the settings window (activating the existing instance), per the
  popup spec's footer requirement — the "unavailable while no settings
  surface exists" scenario no longer applies.

## Capabilities

### New Capabilities

- *(none)* — visual-only change; no observable behavior is added, removed,
  or altered.

### Modified Capabilities

- *(none)*

## Impact

- `src/CLIHub.App/Views/PopupWindow.xaml` — full visual rework to the split
  mockup; `PopupWindow.xaml.cs` — nothing behavioral, at most size-related
  constants.
- `src/CLIHub.App/ViewModels/PopupViewModel.cs` — settings-open command
  wiring; `App.xaml.cs` — passes the single-instance settings shower into
  the popup view model.
- `src/CLIHub.App/App.xaml` — additional shared/popup row styles.
- `src/CLIHub.App/Interop/PopupPositioner.cs` — may need the new window
  size honored when clamping on-screen position (no algorithm change).
- `docs/ui.md` — implementation snapshot updated after the change lands.
- No Core changes; UI-only, verified manually per repo convention.

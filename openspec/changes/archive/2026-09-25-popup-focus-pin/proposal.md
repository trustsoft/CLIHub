# proposal: popup-focus-pin

## Why

The popup hides whenever it loses focus. A user who is actively working from a
launcher (copying a command, reading agent status, comparing projects) loses it
the moment they click anywhere else. A pin toggle lets the user keep the popup
open while they work in another window.

## What Changes

- Add a pin toggle button to the popup footer: pressed (pinned) means the popup
  no longer hides on focus loss; released (default) keeps the current behavior.
- `Esc`, a launch/action click, and the modal dialogs keep their current
  behavior: the popup still hides on `Esc` and on item actions regardless of the
  pin state. `Esc` does not reset the pin.
- The pin state is sticky for the application session: once enabled, it survives
  hide/show cycles (including hotkey re-opens) until the application exits. It is
  not persisted to `settings.json`.
- Re-enabling focus: pressing the hotkey re-activates a pinned popup as usual.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `popup`: the "Close on Esc and focus loss" requirement changes so that hiding
  on focus loss is suppressed while the pin toggle is enabled.

## Impact

- `src/CLIHub.App/Views/PopupWindow.xaml` + `PopupWindow.xaml.cs` — toggle button
  in the footer (reuses `PopupIconButton`), a pinned flag guarding `OnDeactivated`
  (same pattern as `_suppressHide`).
- No changes in `CLIHub.Core` and no new dependencies.
- Manual verification only: UI code-behind in the App layer is not covered by the
  Core test suite (per project convention).
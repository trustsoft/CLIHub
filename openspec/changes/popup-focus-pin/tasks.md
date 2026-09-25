## 1. Pin toggle button in the footer

- [x] 1.1 Add a pin toggle Button as the first element of the footer's right action stack in `PopupWindow.xaml` (before "Open data folder"), reusing `PopupIconButton`, with Segoe MDL2 glyph `E718`, initial `ToolTip` "Pin", and a `Click` handler; verify the app builds with `dotnet build CLIHub.sln` and the button appears in the popup footer
- [x] 1.2 Make the enabled state visually distinct via the button's pressed/toggled styling and switch the glyph to `E196` (Unpin) with `ToolTip` "Unpin" when pinned; verify the visual change is noticeable and the button stays keyboard-focusable (Tab + Space)

## 2. Focus-loss behavior

- [x] 2.1 Add a `private bool _pinned` field to `PopupWindow` and flip it in the toggle handler; set the glyph/tooltip to match the new state; verify the handler toggles the field and runs on the UI thread with no errors
- [x] 2.2 Update `OnDeactivated` to call `Hide()` only when `!_suppressHide && !_pinned`; verify with the app running that unpinned popup hides on focus loss as before, and pinned popup stays visible when clicking another window, while Esc still hides it
- [x] 2.3 Verify text entry/`Scryer`-style cases unchanged: folder picker and confirm dialogs still hide-guard (`_suppressHide`) independently of the pin; verify a pinned popup does not stay open over the folder dialog incorrectly, and `dont run build artifacts` - run `dotnet build CLIHub.sln` and confirm it succeeds before finishing

## 3. Session semantics

- [x] 3.1 Verify the pin survives hide/show and hotkey re-open within one session (enable pin, Esc-hide, press the hotkey again - popup reopens pinned), and that after an application restart the toggle defaults to off; confirm no settings persistence was added (no `settings.json` write from this feature)
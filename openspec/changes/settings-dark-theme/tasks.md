# Tasks

## 1. Theme

- [x] 1.1 Create `src/CLIHub/Themes/SettingsStyles.xaml` (merging LaunchTheme, Sizing, DarkTooltips) with the keyed styles: section label, hint, dark input field, checkbox, footer strip, accent and neutral footer buttons, segmented control, hotkey chips, and the accent status line; add the single `DangerBrush` token to `LaunchTheme.xaml` if needed; verify the solution builds

## 2. Window

- [x] 2.1 Rebuild `SettingsWindow.xaml` on the dark theme per the mockup: drawn header (title, close, drag), dark sections in the existing order (Startup, Default runtime, Global hotkey, Agents probe, Updates — plus Appearance), footer with brand + version chip + Save/Cancel, Escape closes; keep `CanResize` and the existing window size; adjust `SettingsWindow.xaml.cs` for the chrome (round corners, Escape) while keeping the capture handler; verify the solution builds and the window opens from the tray

## 3. Controls

- [x] 3.1 Replace the runtime and path-display combo boxes with segmented controls bound to the existing selection properties, rendering short tokens (`cmd`, `ps`, `wt`); verify selection and Save still apply the right preference
- [x] 3.2 Add the hotkey chips presentation: computed `HotkeyParts` in `SettingsViewModel` (split of `HotkeyText`), chip rendering in the capture field with the hint below; add a unit test for the split formatter if it lives in Core; verify capture (press a combination, Esc cancels) still works

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` and confirm all tests pass
- [x] 4.2 Manually verify against the mockup: dark surfaces and drawn header (drag, close, Escape, rounded corners), section labels and hints, segmented selectors (accent on the selected segment), hotkey chips including a named-key hotkey (e.g. Space or F-key), probe fields, checkboxes, "Check for updates" button and accent status line, footer layout, Save/Cancel semantics (Cancel discards), validation error readability, and the launch window plus What's New unaffected

### Follow-up polish from the visual pass (same scope, user-directed)

- Scrollbar themed via the shared `DarkScrollBar.xaml` merged at the application level (window-scoped implicit styles do not reach ScrollViewer/ListBox template parts); slimmed to 4px (`MinWidth=0` required to beat the template's minimum)
- Probe input fields fixed at 30px height, 4px horizontal text inset, width bound to the "Timeout, seconds" caption
- Segment group geometry: 2px gaps between segments and to the group border
- Hotkey field: auto-width to content, 8px insets on all sides, chip trailing margin compensated so left/right insets match
- Footer: brand label matches the launch window's (BrandBrush, Medium, 15), version chip styles copied from the launch window
- Checkboxes auto-width, left-aligned
- Window sized 420 wide (the minimum) with its height following the launch window's current height

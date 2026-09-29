# Tasks

## 1. Tokens: palette and sizing

- [ ] 1.1 Rewrite `src/CLIHub/Themes/LaunchTheme.xaml` with the reference token set (chrome surface, translucent pane strip, hairlines, field strokes, hover and pressed overlays, accent selection, accent, text roles) and verify `dotnet build CLIHub.sln` succeeds
- [ ] 1.2 Add `src/CLIHub/Themes/Sizing.xaml` with the repeated sizes and corner radii from the reference, merge it from `LaunchWindowStyles.xaml`, and verify the values build and the application starts
- [ ] 1.3 Verify that the Settings window still renders with its light styling after the palette swap, and confirm no implicit style leaks out of the launch window

## 2. Chromeless shell

- [ ] 2.1 Switch the launch window to a chromeless shell — no OS title bar, a 1px outer stroke, a 1px top inset highlight, DWM rounded corners through a guarded interop call, `Segoe UI Variable Text` with a `Segoe UI` fallback — and verify by screenshot that the window shows no title bar and has rounded corners
- [ ] 2.2 Set the window to a fixed width with content-driven height, cap the height against the monitor work area, and verify with many projects and agents that the window height follows the content and both lists scroll inside the panes
- [ ] 2.3 Add pointer-monitor positioning on every show (`GetCursorPos`, `MonitorFromPoint`, `GetMonitorInfo`, device-independent conversion) without `System.Windows.Forms`, wire it into tray, hotkey, second-instance activation and startup shows, and verify the window opens centered on the monitor with the pointer
- [ ] 2.4 Verify the window cannot be moved or resized by dragging while the pane splitter still resizes the panes

## 3. Popup behavior and pin

- [ ] 3.1 Make the window stay out of the taskbar and always on top, and verify there is no taskbar button and it stays above other windows
- [ ] 3.2 Hide the window when it loses focus, suppress that while a dialog opened from the window is active, and verify the folder picker and the removal confirmation keep the window visible
- [ ] 3.3 Add Escape-to-hide and verify the window hides while the application keeps running
- [ ] 3.4 Add the footer Pin control with persisted state in `AppPreferences`, verify that a pinned window survives losing focus, that an unpinned one hides, and that the state survives a restart
- [ ] 3.5 Verify that closing the window through the OS (Alt+F4) still hides to tray instead of exiting the application

## 4. Metrics and rows

- [ ] 4.1 Apply the reference pane metrics — 32px headers, 10px pane padding, hairline divider with a wider transparent splitter hit area, 42px footer — and verify the headers stay aligned, the pane bodies end at the footer, and the divider is a 1px line that still drags
- [ ] 4.2 Restyle the project rows (42px rounded thumbnail on a translucent plate, name and dimmed path per the metrics) and verify with a favorite and a non-favorite project that the marker, the selection accent and the row metrics match the mockup
- [ ] 4.3 Restyle the agent rows (60px, logo, name, version, 28px vector launch/resume buttons) and verify against the mockup that unavailable agents stay dimmed, unsupported actions stay disabled, and an unselected row's action applies to its own row
- [ ] 4.4 Rebuild the footer to the reference metrics — brand, version chip, update-check control immediately to its right, centered status text, icon actions for open data folder, Settings, exit and Pin — wire the open-data-folder and pin commands, and verify the folder opens in the file browser, the pin toggles, and the buttons stay in place as the status message changes

## 5. Actions menus

- [ ] 5.1 Replace the pane `Menu` controls with the `Actions` button and an explicitly styled dark context menu aligned to the button's right edge, and verify both menus open dark, aligned and inside the window
- [ ] 5.2 Add an icon glyph to every menu entry and verify each entry shows its icon and still performs its action

## 6. Path display preference

- [ ] 6.1 Add `PathDisplayStyle` and `PathDisplayStyles.Parse` to `CLIHub.Core.Models` plus a `LeftTrimFormatter` beside the middle-ellipsis formatter, and verify `dotnet build CLIHub.sln` succeeds
- [ ] 6.2 Add xUnit tests for left trim (long path, short path, tiny budget, separator handling) and for parsing valid, invalid and missing style values, and verify `dotnet test CLIHub.sln` passes
- [ ] 6.3 Add `AppPreferences.PathDisplayStyle` with the left-trim default, replace `MiddleEllipsisConverter` with a style-aware `PathDisplayConverter`, bind the project row to it, and verify both modes render correctly with long and short paths
- [ ] 6.4 Add the Appearance path display control to the Settings window and view model, apply a saved change to the running launch window through `IPreferenceApplier`, and verify persistence, immediate application, cancel-discards, and that an invalid stored value falls back to left trim

## 7. Documentation and verification

- [ ] 7.1 Update `ui/mockups/README.md`, `docs/architecture.md` (window shell, tokens, formatters, preference) and `docs/vision.md` for the delivered shell, and verify that every file it names exists
- [ ] 7.2 Walk the `ui/mockups/popup-split.png` element checklist against the running application and verify every element is present or explicitly deferred by the design
- [ ] 7.3 Run the regression pass and verify the tray icon and menu, the global hotkey toggle, single-instance enforcement, hide to tray, the startup-window-visibility preference and the second-instance activation all still work with the popup shell
- [ ] 7.4 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` and verify both succeed, including the code-style enforcement in the build
- [ ] 7.5 Verify `openspec validate "launch-window-chrome"` passes and that the change's deltas match the delivered behavior

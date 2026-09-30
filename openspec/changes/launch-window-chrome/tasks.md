# Tasks

## 1. Tokens: palette and sizing

- [x] 1.1 Rewrite `src/CLIHub/Themes/LaunchTheme.xaml` with the reference token set (chrome surface, translucent pane strip, hairlines, field strokes, hover and pressed overlays, accent selection, accent, text roles) and verify `dotnet build CLIHub.sln` succeeds
- [x] 1.2 Add `src/CLIHub/Themes/Sizing.xaml` with the repeated sizes and corner radii from the reference, merge it from `LaunchWindowStyles.xaml`, and verify the values build and the application starts
- [x] 1.3 Verify that the Settings window still renders with its light styling after the palette swap, and confirm no implicit style leaks out of the launch window

## 2. Chromeless shell

- [x] 2.1 Switch the launch window to a chromeless shell — no OS title bar, a 1px outer stroke, a 1px top inset highlight, DWM rounded corners through a guarded interop call, `Segoe UI Variable Text` with a `Segoe UI` fallback — and verify by screenshot that the window shows no title bar and has rounded corners
- [x] 2.2 Set the window to a fixed width with content-driven height, cap the height against the monitor work area, and verify with many projects and agents that the window height follows the content and both lists scroll inside the panes
- [x] 2.3 Add pointer-monitor positioning on every show (`GetCursorPos`, `MonitorFromPoint`, `GetMonitorInfo`, device-independent conversion) without `System.Windows.Forms`, wire it into tray, hotkey, second-instance activation and startup shows, and verify the window opens centered on the monitor with the pointer
- [x] 2.4 Verify the window cannot be moved or resized by dragging while the pane splitter still resizes the panes

## 3. Popup behavior and pin

- [x] 3.1 Make the window stay out of the taskbar and always on top, and verify there is no taskbar button and it stays above other windows
- [x] 3.2 Hide the window when it loses focus, suppress that while a dialog opened from the window is active, and verify the folder picker and the removal confirmation keep the window visible
- [x] 3.3 Add Escape-to-hide and verify the window hides while the application keeps running
- [x] 3.4 Add the footer Pin control with persisted state in `AppPreferences`, verify that a pinned window survives losing focus, that an unpinned one hides, and that the state survives a restart
- [x] 3.5 Verify that closing the window through the OS (Alt+F4) still hides to tray instead of exiting the application

## 4. Metrics and rows

- [x] 4.1 Apply the reference pane metrics — 32px headers, 10px pane padding, hairline divider with a wider transparent splitter hit area, 42px footer — and verify the headers stay aligned, the pane bodies end at the footer, and the divider is a 1px line that still drags
- [x] 4.2 Restyle the project rows (42px rounded thumbnail on a translucent plate, name and dimmed path per the metrics) and verify with a favorite and a non-favorite project that the marker, the selection accent and the row metrics match the mockup
- [x] 4.3 Restyle the agent rows (60px, logo, name, version, 28px vector launch/resume buttons) and verify against the mockup that unavailable agents stay dimmed, unsupported actions stay disabled, and an unselected row's action applies to its own row
- [x] 4.4 Rebuild the footer to the reference metrics — brand, version chip, update-check control immediately to its right, centered status text, icon actions for open data folder, Settings, exit and Pin — wire the open-data-folder and pin commands, and verify the folder opens in the file browser, the pin toggles, and the buttons stay in place as the status message changes
- [x] 4.5 Make the Projects pane hug its list (content-sized column with a 300px list minimum), stretch every row to the full list width, keep the path elastic so it is shortened to the list width instead of widening it, and verify by measuring screenshots that the divider sits next to the list, that the rows are uniform, and that dragging the divider widens the list and expands the paths
- [x] 4.6 Raise the Projects pane minimum so it can never clip its list, and shorten paths by measuring the largest fitting form with the row's own typeface (with the favourite-marker reserve applied only when a favourite is shown); verify by dragging the divider in as far as it goes that the divider stops at the pane minimum, that the row surfaces keep their rounded corners, and that a shortened path fills the row
- [x] 4.7 Size the Agents pane to its list, stretch every agent row to the full list width with its action buttons right-aligned and at least 24px after the row text, let the window fit its content, and verify by screenshot that the pane hugs the list, that the rows are uniform with aligned buttons, and that the window has no empty space beside the panes
- [x] 4.8 Refactor the launch window XAML and view model for reuse without changing anything visible: shared pane header/surface/menu-icon/footer-icon/separator/glyph resources, the pane-header margin as a token, removal of unused palette entries, tokens and element names, one dialog-owner helper, and no dead per-refresh work; verify with a build, the test suite and a pixel comparison of the window interior against a pre-refactor screenshot

## 5. Actions menus

- [x] 5.1 Replace the pane `Menu` controls with the `Actions` button and an explicitly styled dark context menu aligned to the button's right edge, and verify both menus open dark, aligned and inside the window
- [x] 5.2 Add an icon glyph to every menu entry and verify each entry shows its icon and still performs its action

## 6. Path display preference

- [x] 6.1 Add `PathDisplayStyle` and `PathDisplayStyles.Parse` to `CLIHub.Core.Models` plus a `LeftTrimFormatter` beside the middle-ellipsis formatter, and verify `dotnet build CLIHub.sln` succeeds
- [x] 6.2 Add xUnit tests for left trim (long path, short path, tiny budget, separator handling) and for parsing valid, invalid and missing style values, and verify `dotnet test CLIHub.sln` passes
- [x] 6.3 Add `AppPreferences.PathDisplayStyle` with the left-trim default, replace `MiddleEllipsisConverter` with a style-aware `PathDisplayConverter`, bind the project row to it, and verify both modes render correctly with long and short paths
- [x] 6.4 Add the Appearance path display control to the Settings window and view model, apply a saved change to the running launch window through `IPreferenceApplier`, and verify persistence, immediate application, cancel-discards, and that an invalid stored value falls back to left trim

## 7. Documentation and verification

- [x] 7.1 Update `ui/mockups/README.md`, `docs/architecture.md` (window shell, tokens, formatters, preference) and `docs/vision.md` for the delivered shell, and verify that every file it names exists
- [x] 7.2 Walk the `ui/mockups/popup-split.png` element checklist against the running application and verify every element is present or explicitly deferred by the design
- [x] 7.3 Run the regression pass and verify the tray icon and menu, the global hotkey toggle, single-instance enforcement, hide to tray, the startup-window-visibility preference and the second-instance activation all still work with the popup shell
- [x] 7.4 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` and verify both succeed, including the code-style enforcement in the build
- [x] 7.5 Verify `openspec validate "launch-window-chrome"` passes and that the change's deltas match the delivered behavior

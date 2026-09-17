# Tasks — Restyle Popup Window

## 1. Shared and popup-specific styles

- [x] 1.1 Add popup styles to `App.xaml`: pane title with `Actions` chevron button (chrome on hover only), project row (42 px thumb, 60 px row, accent selection bar), agent row (28 px logo, stacked name/version, 1 px divider), dark `ContextMenu` popover chrome; verify `dotnet build CLIHub.sln` succeeds

## 2. Window structure

- [x] 2.1 Rework `PopupWindow.xaml` to the split mockup: 543 px window, Projects pane (300 px) + hairline divider + Agents pane, uppercase pane titles with right-aligned `Actions` buttons, dark footer strip; verify hotkey open/close, focus, and deactivate-close still work (manual run)
- [x] 2.2 Size the window with `SizeToContent` + `MaxHeight` caps on both lists; verify long lists scroll inside panes and `PopupPositioner` keeps the window on-screen at 100 %/150 % DPI (manual run)

## 3. Rows and footer

- [x] 3.1 Restyle project rows (logo 42 px, name/path stacked, selected = accent background + 3 px accent bar) keeping Add/Remove menu bindings; verify add/remove flows work (manual run)
- [x] 3.2 Restyle agent rows (logo 28 px, name with version below, subtle row divider, icon-only run button honoring `CanRun`); verify run launches and unavailable agents render disabled (manual run)
- [x] 3.3 Restyle the footer: brand + version chip left, status centered, icon buttons (open-data-folder, settings, exit) in dark icon style; verify footer actions work (manual run)
- [x] 3.4 Enable the footer settings button: `PopupViewModel` gets an open-settings command bound to the app's single-instance `ShowSettings`; verify activating it opens (or activates) the settings window (manual run)

## 4. Verification and docs

- [x] 4.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; both must pass with no Core changes
- [x] 4.2 Compare the running popup against `docs/ui/popup-split.png` and fix visual deltas (spacing, sizes, colors, label case)
- [x] 4.3 Update the implementation-snapshot section of `docs/ui.md` (popup now matches the split mockup; resume/`Update`/`Init` submenus and the settings footer button remain future changes)

# Proposal

## Why

The delivered launch window matches the mockup's structure but not its shell. It still uses the standard Windows title bar, a coarse 5px pane divider, ad-hoc spacing and a palette that was sampled by hand, and it behaves like an ordinary application window: it appears in the taskbar, does not stay above other windows, and never hides itself. The approved popup design — exercised by the reference implementation in the sibling `CLIHub 2` project (`Views/PopupWindow.xaml(.cs)`, `Assets/Styles/Palette.xaml`, `Templates/Popup.xaml`) — defines a chromeless popup with a token-based palette and fixed metrics, which is what the mockup shows. Until the shell matches, the launcher neither looks nor behaves like the approved design.

## What Changes

- **Chromeless shell**: no OS title bar or frame; a 1px outer stroke, a 1px inset highlight along the top edge, and OS-rounded window corners; the window is not movable, has a fixed width and content-driven height.
- **Popup behavior**: no taskbar entry, always on top, hides when it loses focus unless pinned, hides on Escape, positioned centered on the work area of the monitor that contains the pointer every time it is shown, and a **Pin** control in the footer whose state persists across sessions.
- **Palette and tokens**: adopt the reference token set (chrome surface, translucent pane strip, hairlines, field strokes, hover/pressed overlays, accent selection, accent) and extract shared sizing tokens.
- **Metrics**: 32px pane headers, 10px pane padding, 1px pane divider drawn as a hairline while the splitter keeps a wider drag area, 42px project thumbnails with a 9px radius on a translucent plate, 60px agent rows, 28px vector action buttons, 42px footer with the brand, version chip, update-check control, centered status and icon actions, and `Segoe UI Variable Text` with a `Segoe UI` fallback.
- **Actions menus**: opened from the pane `Actions` control, right-aligned with it, with an icon on every entry.
- **Path display preference**: a new setting with `Left trim` (default, the mockup's `...tail` presentation) and `Middle ellipsis` (both ends stay visible), applied immediately and persisted.
- Keep the draggable pane splitter, the footer's update-check control next to the version chip, and add the reference's **open data folder** action to the footer icon actions.

## Capabilities

### New Capabilities

<!-- none: the shell behavior belongs to the existing lifecycle and layout capabilities -->

### Modified Capabilities

- `main-window-layout`: adds the chromeless window chrome, the hairline pane divider with a wider drag area, the actions-menu presentation, and makes the project row's path presentation follow the path display preference.
- `app-lifecycle`: adds the launch window's popup shell behavior — no taskbar entry, always on top, hiding on focus loss unless pinned, hiding on Escape, pin control with persisted state, and pointer-monitor positioning on every show.
- `preferences-ui`: adds the path display preference with its control, persistence, immediate application and default.

## Impact

- **UI**: `src/CLIHub/Windows/LaunchWindow.xaml(.cs)` (shell, metrics, menus, pin control, vector glyphs), `src/CLIHub/Themes/` (palette tokens replaced, sizing tokens added, window-scoped styles updated), `src/CLIHub/Converters/` (path display converter gains the style input), `src/CLIHub/Interop/User32.cs` (window rounding, monitor work area and pointer position — no `System.Windows.Forms`), `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` (pin state, path display style), `src/CLIHub/Windows/SettingsWindow.xaml` + `ViewModels/SettingsViewModel.cs` (path display control).
- **Core**: a left-trim formatter beside the existing middle-ellipsis formatter, a `PathDisplayStyle` value with parsing, and the new `AppPreferences.PathDisplayStyle` — with unit tests.
- **No changes** to plugin descriptors, the tray menu structure, or the update mechanism.
- **Config**: one additive preference (`pathDisplayStyle`); existing configurations keep working and default to left trim.
- **Docs**: `ui/mockups/README.md`, `docs/architecture.md`, `docs/vision.md`.
- **Non-goals**: restyling the Settings window or the tray menu, installing updates from the tray, changing keyboard handling beyond the Escape-to-hide behavior, and exposing the pin anywhere other than the footer.

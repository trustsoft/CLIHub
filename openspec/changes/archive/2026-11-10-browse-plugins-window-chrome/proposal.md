## Why

The Browse Plugins dialog currently lacks full window manipulation affordances: it cannot be resized by the user or moved by dragging the header, has no rounded corners, and does not respond to Escape. These limitations make the dialog difficult to use when the plugin list is long or descriptions are extensive. Upgrading the dialog with drawn chrome (header drag, resize borders, rounded corners, and Escape-to-close) provides essential manipulation capabilities while remaining a modal, owned dialog that does not appear in the taskbar.

## What Changes

- Upgrade the Browse Plugins dialog with drawn chrome: user-resizable with 6px resize borders, header drag to move, rounded corners via `DwmApi.TryRoundCorners`, and WM_GETMINMAXINFO hook to enforce MinWidth/MinHeight as the minimum tracking size with DPI scaling
- Add Escape-to-close (no text input exists in the dialog)
- Keep `ShowInTaskbar="False"`, `ShowDialog()` modality, and `CenterOwner` startup location
- Reduce the dialog's default size to 640x480 (was 800x600) and minimum size to 520x360 (was 600x400)
- Header contains only the close button (no maximize/restore button)

## Capabilities

### New Capabilities

<!-- None: this is a window chrome upgrade, not a new capability. -->

### Modified Capabilities

- `plugin-feed`: adds a requirement describing the Browse Plugins dialog's drawn chrome (header drag, user resizing, rounded corners, Escape closes, not shown in taskbar, modal)

## Impact

**Modified code:**
- `src/CLIHub/Views/BrowsePluginsDialog.xaml` — change `Height` to 480, `Width` to 640, `MinHeight` to 360, `MinWidth` to 520; change `ResizeMode` to `CanResize`; add `SourceInitialized="OnSourceInitialized"` and `KeyDown="OnKeyDown"`; change `WindowChrome.ResizeBorderThickness` to 6; add `MouseLeftButtonDown="OnHeaderMouseDown"` to the header Grid
- `src/CLIHub/Views/BrowsePluginsDialog.xaml.cs` — add `OnSourceInitialized` (calls `DwmApi.TryRoundCorners` and adds `HwndSource` hook for WM_GETMINMAXINFO to enforce DPI-scaled minimum tracking size), add `OnHeaderMouseDown` (calls `DragMove()` for left button and swallows `InvalidOperationException`), add `OnKeyDown` (closes on Escape)

**Reuses:**
- `DwmApi.TryRoundCorners` and `User32` P/Invoke declarations from `src/CLIHub/Interop/`
- The `WindowCloseButton` style and the header drag pattern from `SettingsWindow`

**No breaking changes** — the dialog's external contract (`ShowBrowsePluginsDialog()`, modality, owner) remains unchanged. Users gain window manipulation capabilities; the default size is now smaller.

## Dependencies

<!-- None: all referenced interop code and WPF resources already exist. -->

## Non-Goals

- Changing `ShowInTaskbar` (explicitly kept `False`)
- Changing the dialog's modality or `ShowDialog()` entry point
- Adding maximize/restore or minimize buttons
- Redesigning the dialog's content or layout

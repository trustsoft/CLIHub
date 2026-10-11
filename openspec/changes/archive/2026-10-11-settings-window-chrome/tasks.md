## 1. Glyphs and styles

- [x] 1.1 Add maximize/restore (and any needed terminal) constants to `IconGlyphs`; verify the app builds.
- [x] 1.2 Reuse the close-button visual pattern for the icon-only title-bar buttons (no new style was needed); verify they render dark with hover.

## 2. Title bar

- [x] 2.1 Rebuild the `SettingsWindow.xaml` header as a title bar: application icon, "CLIHub - Settings" title, then maximize/restore and close buttons (no version pill, no minimize button); verify the window builds and the header still drags.
- [x] 2.2 In `SettingsWindow.xaml.cs`, add maximize/restore (toggling `WindowState` and glyph) and close handlers; keep `OnHeaderMouseDown`, DWM rounding, and Escape handling; verify behavior manually.
- [x] 2.3 Update the chrome tests where practical (title text/glyphs present) and keep the existing window tests green.
- [x] 2.4 Handle `WM_GETMINMAXINFO` in the Settings window's `HwndSource` hook and clamp the maximized size/position to the monitor work area (add `MonitorFromWindow`/`MINMAXINFO` to `User32`); verify the footer stays above the taskbar when maximized.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 3.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 3.3 Run `openspec validate settings-window-chrome --strict` and verify the change is valid.
- [x] 3.4 Smoke-test: the title bar shows the icon and "CLIHub - Settings" with max/close only (no version pill, no minimize); maximize/restore works; drag and Escape still work; the footer Save/Cancel is unchanged.

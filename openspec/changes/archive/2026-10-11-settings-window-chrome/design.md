## Context

See `proposal.md` — Why. The current `SettingsWindow.xaml` draws its own chrome (`WindowStyle=None` + `WindowChrome`), with a header grid (`WindowHeader` style) holding a "Settings" title and a single close button (`WindowCloseButton` style + `IconGlyphs.Close`), drag handling in `OnHeaderMouseDown`, DWM rounding in `OnSourceInitialized`, and Escape-to-close in `OnKeyDown`. `SettingsViewModel.Version` already exposes the running version and is bound in the footer. `IconGlyphs` holds the shared MDL2 constants (Add, Delete, Refresh, Exit, Close, …).

## Goals / Non-Goals

**Goals:**

- A target-faithful title bar (icon, "CLIHub - Settings", maximize/restore/close) with no change to save behavior or page content.
- Keep the existing drag, DWM rounding, resize border, and Escape behavior.

**Non-Goals:**

- Status footer, Apply Changes, search, cards, badges, profile card, Mica.

## Decisions

### D1 — Keep drawn chrome and add buttons, rather than adopt the OS title bar

The window keeps `WindowStyle=None` + `WindowChrome` and draws the new buttons. **Why:** the whole app uses drawn dark chrome; switching to the native title bar would drop the dark look and the existing `WindowChrome` resize/rounding setup. **Alternative rejected:** `WindowStyle=SingleBorderWindow` — light OS chrome that breaks the theme.

### D2 — Draw minimize / maximize-restore / close as icon buttons

Reuse the existing close-button pattern and add a small `TitleBarButton` style for the maximize/restore button (plus close), grouped top-right. The maximize button toggles `WindowState` between `Maximized` and `Normal` and swaps its glyph based on `WindowState`. **Why:** consistent with the drawn close button and testable via the code-behind handlers. **Alternative rejected:** `SystemCommands` chrome — still needs the drawn surface. The minimize button and the version pill are intentionally omitted per the agreed scope.

### D3 — Title bar identity from existing resources

The title bar shows the application icon — `app.ico`, which is already an embedded WPF resource (`<Resource Include="app.ico" />`) — via its pack URI (`pack://application:,,,/app.ico`); `Window.Icon` is set to the same URI so the taskbar and Alt-Tab also use it. The title is the literal "CLIHub - Settings". **Why:** the drawn icon matches the real application icon and there is one source. The version pill is dropped and the footer keeps the version display.

### D4 — Preserve behavior and tests

Header drag, DWM rounding, Escape-to-close, and the Save/Cancel footer are unchanged. Chrome is verified by the existing window tests plus new assertions for the title text/glyphs where practical. **Why:** the change is presentational; behavior must not regress.

### D5 — Maximize respects the monitor work area

A `WindowStyle=None` + `WindowChrome` window maximized by the OS covers the full monitor, so the taskbar hides the footer. Handle `WM_GETMINMAXINFO` in an `HwndSource` hook and set `ptMaxSize`/`ptMaxPosition` from the window monitor's work area (`MonitorFromWindow` + `GetMonitorInfo`, both already in `User32`). **Why:** it is the standard, multi-monitor-correct fix and keeps the footer visible. **Alternative rejected:** setting `MaxHeight` from `SystemParameters.WorkArea` — only correct for the primary monitor and does not position the window.

## Risks / Trade-offs

- **Maximize changes layout expectations** → the content already uses a `ScrollViewer`; maximizing only enlarges surfaces. Sidebar width is fixed (220), content flexible.
- **Glyph availability in Segoe MDL2** → pick well-known codes (maximize/restore) and fall back to existing constants if a glyph looks off.

## Migration Plan

- None; presentational only.

## Open Questions

- None.

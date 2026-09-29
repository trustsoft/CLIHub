# Design

## Context

See `proposal.md` — Why. Current state and constraints that shape the approach:

- The delivered launch window is an ordinary WPF `Window` with the system title bar, a 5px `GridSplitter` between the panes, hand-picked hex colours in `Themes/LaunchTheme.xaml`, hand-picked sizes inline in the window XAML, text-character action buttons (`▶`, `↻`), and `Menu`/`MenuItem` pane actions.
- The reference implementation in the sibling `CLIHub 2` project defines the target shell: `Views/PopupWindow.xaml` (`WindowStyle=None`, `ResizeMode=NoResize`, `ShowInTaskbar=False`, `Topmost`, `SizeToContent=Height`, `WindowStartupLocation=Manual`, 1px `FieldStroke` frame, 1px top inset highlight), `PopupWindow.xaml.cs` (DWM rounding, hide-on-deactivate with a `_suppressHide` guard, Escape hides, in-memory pin), `Assets/Styles/Palette.xaml` (token palette), `Assets/Styles/Sizing.xaml` (size tokens), `Templates/Popup.xaml` (row templates), `Interop/PopupPositioner.cs` (pointer-monitor centering), `Assets/Converters/PathLeftTrimConverter.cs` (left trim to a fixed 33 characters).
- The reference is a fixed 300px left column with no splitter, while this project's durable `main-window-layout` requires a draggable splitter with minimum pane widths — so the shell is adopted, the splitter is kept.
- The reference's `PopupPositioner` uses `System.Windows.Forms.Screen`, which this repository bans (`AGENTS.md`); the equivalent must be built on `user32` interop.
- `CLIHub.Core` holds the middle-ellipsis formatter with unit tests, and `tests/CLIHub.Tests` references Core only, so new pure formatting logic belongs in Core to stay testable.
- `AppPreferences` stores runtime, hotkey and probe settings as strings parsed with helpers (`RuntimeKinds.Parse`), which is the pattern to mirror for a path display style.

## Goals / Non-Goals

**Goals:**

- Match the mockup's shell: chromeless, rounded, hairline-separated, popup-behaved, and sized by content.
- Replace hand-picked colours and sizes with the reference token sets so later visual work changes tokens, not controls.
- Keep the delivered behavior that durable specs require: draggable splitter with minimum widths, scrolling lists, footer actions with the update check next to the version chip, keyboard-free operation from the tray and the hotkey.
- Keep path shortening testable in Core and selectable from the Settings window.

**Non-Goals:**

- No restyle of the Settings window or the tray menu.
- No change to the update mechanism, plugin descriptors, or the seed data.
- No `System.Windows.Forms` dependency, and no window-position memory: the popup always re-centres (see Decisions).
- No pin control outside the footer.

## Decisions

**1. Adopt a chromeless shell instead of keeping or custom-drawing a title bar.**
`WindowStyle="None"` with a 1px stroke, a 1px top inset highlight, and DWM rounded corners matches the mockup and the reference. *Alternatives:* keeping the OS title bar (rejected — the mockup shows none and it fights the dark theme), or `WindowChrome` with a custom title-bar strip (rejected — the popup needs no caption, minimize, maximize or close controls, since Escape, the tray menu and the footer Exit already cover putting the window away).

**2. Keep the draggable splitter, drawn as a hairline.**
The durable `main-window-layout` requires the splitter and minimum pane widths, so the `GridSplitter` stays; it becomes transparent with a wider hit area, and the visible separation is a 1px line. *Alternative:* the reference's fixed 300px column (rejected — it would drop a durable requirement).

**3. Fixed width with content-driven height.**
The window uses a fixed width, `SizeToContent="Height"`, and a `MaxHeight` on both lists so long lists scroll inside the panes; the height is capped against the work area of the monitor it is shown on so a short screen cannot clip the footer. *Alternative:* the current freely resizable window (rejected per the mockup and the reference).

**4. Adopt the popup behavior wholesale, and persist the pin.**
No taskbar entry, always-on-top, hide on focus loss unless pinned, Escape hides. The pin state is stored in `AppPreferences` rather than kept in memory as the reference does, so a pinned window does not surprise the user by reverting to auto-hide after a restart. *Alternative:* in-memory pin (rejected — inconsistent with the other persisted window preferences).

**5. Pointer-monitor positioning without Windows Forms.**
`GetCursorPos`, `MonitorFromPoint` and `GetMonitorInfo` come from `Interop/User32.cs`; the work-area rectangle is converted from physical to device-independent units through the window's `HwndSource.CompositionTarget.TransformFromDevice`, then the window is centred and clamped inside the work area. *Alternative:* `System.Windows.Forms.Screen.FromPoint` (rejected — banned by project constraints).

**6. The reference palette becomes this project's palette.**
`Themes/LaunchTheme.xaml` is rewritten with the reference tokens (`ChromeBg`, translucent `StripBg`, `Hairline`, `FieldStroke`, `HoverOverlay`, `PressedOverlay`, `AccentSelection`, `Accent`, text roles), and `Themes/Sizing.xaml` is added for repeated sizes and corner radii. The window-scoped styles keep living in `Themes/LaunchWindowStyles.xaml` merged from `LaunchWindow.xaml`, so the Settings window is unaffected. The durable `launch-window-theme` capability describes contrast and theme scope rather than hex values, so it needs no delta: the palette swap is implementation.

**7. Pane actions become a button that opens an explicitly styled menu.**
A `Button` styled as `PaneActionsButton` opens a `ContextMenu` whose menu and item styles are referenced by key (a `ContextMenu` is outside the window's visual tree, so implicit window styles would not reach it), aligned with the button's right edge via `HorizontalOffset`, with a Segoe MDL2 glyph per entry. *Alternative:* keeping `Menu`/`MenuItem` with the implicit popup template (rejected — icons and right alignment are awkward, and popup placement already caused friction).

**8. Vector glyphs for the row actions.**
The launch and resume buttons draw their glyphs with `Path`/`Ellipse` geometry filled from the button's foreground, as the reference does, instead of the `▶`/`↻` characters, so they scale with the button and inherit hover/disabled colours.

**9. Path display: two modes in Core, chosen by preference.**
`CLIHub.Core.Models.PathDisplayStyle` (`LeftTrim`, `MiddleEllipsis`) with a `PathDisplayStyles.Parse` helper (default `LeftTrim`), plus a `LeftTrimFormatter` beside the existing `MiddleEllipsisFormatter`; the WPF converter becomes `PathDisplayConverter`, taking the path, the available width and the style. *Alternatives:* a fixed 33-character left trim as in the reference (rejected — it is not width-aware and overflows narrow panes), or middle-ellipsis only (rejected — the setting is required).

**10. Settings gets an "Appearance" path display control.**
`SettingsViewModel` gains the selected style, loaded from and saved into `AppPreferences.PathDisplayStyle`, and `IPreferenceApplier` gains a method that pushes a style change into the running launch window so the setting applies without a restart, mirroring how the hotkey and runtime changes apply today.

**11. The pin lives only in the footer.**
The Pin control is part of the popup shell in the footer and its state is stored in `AppPreferences`; the Settings window is not extended with a pin option, so the window keeps a single, discoverable control for it. *Alternative:* mirroring it in Settings (rejected — two controls for one toggle, and the footer is where the user needs it).

**12. The footer's open-data-folder action uses the shell.**
The action opens `%APPDATA%\CLIHub` through the operating system's shell (`Process.Start` with `UseShellExecute`), so the default file browser handles it. *Alternatives:* reusing `IProcessLauncher` (rejected — it spawns terminals with a working directory, not the file browser), or `System.Windows.Forms.Process`-style helpers (rejected — banned).

## Risks / Trade-offs

- **A chromeless window loses the familiar close button and cannot be moved** → Escape, the footer Exit, the tray menu and the hotkey all remain; the window re-centres on the pointer's monitor every time it is shown, so its position is predictable; verify that Alt+F4 still hides to tray rather than exiting.
- **Always-on-top plus hide-on-focus-loss can fight modal dialogs and the tray menu** → guard auto-hide while a dialog opened from the window is active (the reference's `_suppressHide` approach) and verify the folder picker and the removal confirmation both keep the window visible.
- **`SizeToContent="Height"` can exceed a short work area** → cap the height against the monitor work area and keep the lists' `MaxHeight` so they scroll instead of growing the window.
- **DWM rounding is not available on older Windows builds** → call it through a guarded interop method that ignores failures, keeping square corners as the documented fallback.
- **Persisting the pin changes the default feel** → the stored default stays unpinned (auto-hide), so behavior only changes after an explicit pin; verify a fresh config still auto-hides.
- **Styling a `ContextMenu` requires explicit keys** (implicit window styles do not reach it) → keep the menu styles in the same dictionary with keys and reference them explicitly; verify the menu is dark and its entries show icons.
- **A large visual rewrite can regress behaviour quietly** → port in the task order below, screenshot after each group, and re-run the regression pass (tray, hotkey, single instance, hide to tray, startup visibility) at the end.
- **Metric changes cannot be unit-tested** → verify against `ui/mockups/popup-split.png` with screenshots and the element checklist, and keep the pure path formatting covered by Core tests.

## Migration Plan

No data migration: `pathDisplayStyle` is additive and missing or unrecognized values fall back to left trim. Palette and metric changes are code-only. Rollback is a revert of the change; the previous palette, styles and window XAML are preserved in git history and the previously archived launch-window behavior is unchanged by it.

Order of delivery: tokens (palette and sizing) → shell (chromeless frame, size, positioning) → popup behavior and pin → metrics and rows → actions menus → path display preference → docs and the full verification pass.

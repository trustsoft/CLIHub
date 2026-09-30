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

**13. The Settings window is owned by the launch window.**
Because the shell is always on top, a normal Settings window would open *behind* it. `SettingsWindow.ShowSettings` therefore assigns the launch window (`Application.Current.MainWindow`) as its owner, which the desktop window manager keeps above the owner. Verified: Settings is visible above a pinned launcher, and it survives the launcher hiding. *Alternative:* making Settings topmost as well (rejected — it would float above unrelated applications, and the launcher already owns the window's lifetime).

**14. The project list is content-sized with a 300px default, and its pane hugs it.**
The Projects column is `Auto` instead of a star column, so the pane is exactly as wide as its list requires and the divider sits next to the list — the star-sized pane used to leave a gap between the list and the divider. The list is `MinWidth 300` and every row is stretched to the list width, so rows share one width; a long path never widens the list because the path is the elastic part of a row: its character budget is measured from the list's *measured* width minus the fixed insets (padding, thumbnail, favourite reserve). That budget is self-adjusting — each layout pass trims the path a little more, which shrinks the content-driven list until the 300px floor stops it, and when the user drags the divider the pane (and list) is a fixed width that the paths then fill. Agent rows keep a full-width list, because their action buttons are right-aligned to the pane. *Alternatives:* a star-sized pane (rejected — the gap the owner reported) or per-row content-sized surfaces (rejected — rows then had different widths).

**15. Path shortening is measured, not estimated, and the pane minimum covers the list.**
Two defects surfaced while narrowing the pane by hand. First, the path's character budget used an estimated average glyph width plus a reserve for the favourite marker, so a path was shortened earlier than the row required; `PathDisplayConverter` now binary-searches the largest budget whose *measured* text fits the available width, using the row's own typeface (`FormattedText` with the element's DPI), and the marker reserve is added only when the project is a favourite. Second, the Projects pane could be dragged narrower than its list needed, so the pane clipped the list and the row surfaces' rounded corners looked cut off; the pane's minimum is therefore its list's minimum plus the pane padding on both sides (320px). *Alternatives:* keeping the estimate (rejected — the row was visibly under-filled) or letting the list shrink below its minimum (rejected — it clipped the row surfaces).

**16. The Agents pane and its rows are content sized, and the window fits its content.**
The Agents column is `Auto` and the list is as wide as its widest row, so the pane hugs the list instead of stretching to the window. Every agent row is stretched to the list width and its action buttons are right-aligned (a `*` spacer before them), which keeps the buttons aligned across rows as the mockup shows; the 24px margin between the row text and the buttons is the minimum separation the owner asked for, and the remaining slack sits between the text and the buttons. Because both panes are content sized, the window itself uses `SizeToContent="WidthAndHeight"` so no empty space is left beside either pane. *Trade-off:* with both panes sized to their content there is no slack left for the splitter to redistribute, so the splitter resizes the Projects pane alone (the Agents pane keeps its content width) and the window follows — the drag is handled on the window with a hit tolerance around the divider, because a 1px divider is hard to hit and a window resize during a captured drag can drop the capture. *Alternative:* a fixed-width window with a stretched pane (rejected — it leaves the empty space the owner objected to); if the following drag proves unreliable in use, that fallback is the way back.
Two defects surfaced while narrowing the pane by hand. First, the path's character budget used an estimated average glyph width plus a reserve for the favourite marker, so a path was shortened earlier than the row required; `PathDisplayConverter` now binary-searches the largest budget whose *measured* text fits the available width, using the row's own typeface (`FormattedText` with the element's DPI), and the marker reserve is added only when the project is a favourite. Second, the Projects pane could be dragged narrower than its list needed, so the pane clipped the list and the row surfaces' rounded corners looked cut off; the pane's minimum is therefore its list's minimum plus the pane padding on both sides (320px). *Alternatives:* keeping the estimate (rejected — the row was visibly under-filled) or letting the list shrink below its minimum (rejected — it clipped the row surfaces).

**17. Reuse-first XAML, verified by a pixel comparison.**
The repeated constructs in the launch window are single resources: `PaneHeader`, `PaneSurface`, `MenuIcon` (eleven menu entries), `FooterIcon` (four footer glyphs), `MenuSeparator`, `PlayGlyph`, plus `Sizing.Margin.PaneHeader` for the header margin; unused palette brushes, size tokens and element names were deleted, the three dialogs share one `Prompt` helper that owns the always-on-top owner, per-agent status strings and a per-refresh host-install probe that nothing rendered were dropped. Because a refactor must not move a single pixel, it was verified by capturing the window before and after and comparing the interior: 6 of 634,032 pixels differ (0.0009%), all of them the anti-aliasing of the window's own frame, which depends on the window's sub-pixel screen position. *Considered and deliberately not done:* narrowing the path-measurement search with an estimate (the search is already bounded to a handful of measurements per row and only for paths that must be shortened) and touching the Settings window's literals (outside this change's scope).

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

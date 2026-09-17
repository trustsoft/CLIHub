# Design — Restyle Settings Window

## Context

The settings window (`src/CLIHub.App/Views/SettingsWindow.xaml`) is a plain
light StackPanel form inside the default WPF caption. The approved mockup
(`CLIHub 2 UI\settings\settings-mockup.html`) defines the dark chrome-frame
language. The popup window is also still light, so this change introduces the
first shared style resources; no styling infrastructure exists today
(`App.xaml` resources are empty). Behavior lives in `SettingsViewModel` and
`HotkeyCaptureBox` (a read-only `TextBox` subclass) and must not change.

## Goals / Non-Goals

- Goals: settings window visually matches the mockup; visual vocabulary
  (palette, labels, fields, buttons, footer) extracted as reusable resources;
  custom chrome (title bar + close, drag-to-move).
- Non-Goals: restyling the popup window (separate change); any behavior change
  in load/save/validation/hotkey capture; new settings.

## Decisions

- **Chrome via `WindowStyle="None"` + `System.Windows.Shell.WindowChrome`.**
  `WindowChrome` with `CaptionHeight=36` gives drag-to-move and caption
  double-click semantics declaratively; the close button opts out via
  `WindowChrome.IsHitTestVisibleInChrome=True`. `ResizeMode=NoResize` stays.
  Alternative considered: manual `DragMove()` on mouse events — more code for
  the same result.
- **Rounded corners via DWM, not transparency.** On `SourceInitialized`, call
  `DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE=33,
  DWMWCP_ROUND)` (new P/Invoke in `Interop/NativeMethods.cs`). Keeps the
  system shadow, avoids `AllowsTransparency` rendering cost, degrades to
  square corners on pre-Win11. The mockup's 12 px radius is approximated by
  the system radius; accepted.
- **Shared vocabulary in `App.xaml` resources:** color brushes (chrome
  `#26262C`, strip overlay 14 % black, hairline strokes at 9–10 % white, text
  `#FFFFFF`/muted `#9A9A9A`/labels `#8B8B8B`, accent `#4CC2FF`), styles for
  uppercase section labels, dark fields, ghost and accent buttons, and the
  footer strip. Named neutrally (`SettingsChrome*` avoided; use role names)
  so the popup restyle can consume them unchanged.
- **Runtime segmented control = horizontal `ListBox`** bound to the existing
  `Runtime`/`RuntimeOptions` members; items templated as segmented pills
  (selected = lighter pill). RadioButtons would need a string↔bool converter
  per item; the ListBox binds to the view model as-is.
- **Hotkey capture field stays a styled `TextBox`.** `HotkeyCaptureBox`
  inherits TextBox focus/capture logic; re-templating into key chips
  (mockup) would replace the control's visual root and risk capture
  regressions. This change only applies the dark field style and keeps the
  canonical text display; chips are a follow-up presentation refinement with
  no spec impact.
- **Footer:** a bottom strip (14 % black, hairline divider above) with
  brand + version chip on the left (version from the entry assembly) and
  «Отмена» (ghost) / «Сохранить» (accent) on the right, bound to the existing
  commands/handlers.
- **Update status line** binds to the existing `StatusText`; the version
  portion is part of the status string, so accenting only the version is not
  data-bound — the whole line uses muted gray, matching the popup's
  restrained accent use. Accepted deviation from the mockup's blue version.

## Risks / Trade-offs

- [WindowStyle=None removes the system icon/menu] → Only close/drag are
  needed (NoResize, modal-like surface); Alt+F4 still works.
- [DWM rounding is a no-op on older Windows] → Square corners with the same
  1 px border remain correct.
- [Dark theme via implicit styles could leak into future dialogs] → Styles
  are keyed (`Style={StaticResource ...}`) and applied explicitly, not
  implicit per-type.

## Migration Plan

UI-only change; no data or config migration. Rollback = revert the commit.
Manual verification checklist comes from `docs/ui.md` behavior (open/save/
close/hotkey capture) plus visual comparison against
`docs\ui\settings.png`.

## Open Questions

- None.

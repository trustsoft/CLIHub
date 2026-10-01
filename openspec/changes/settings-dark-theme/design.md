# Design

## Context

The Settings window is plain WPF with hardcoded light colors and default controls: OS title bar, two `ComboBox`es, a read-only capture `TextBox`, three `CheckBox`es, and a light footer. The dark language it should adopt already exists — `LaunchTheme.xaml` brushes, `Sizing.xaml` metrics, the `WhatsNewWindow` drawn-chrome pattern (WindowStyle=None, `DwmApi.TryRoundCorners`, header drag, Escape close), `DarkContextMenu`/`NotesInstallButton` control templates, and the `SectionLabel`/hint styles in `LaunchWindowStyles.xaml`. The mockup maps every element onto these existing pieces.

## Goals / Non-Goals

**Goals:**
- Settings reads as the same application as the launch window and What's New.
- Palette discipline: reuse existing brushes; at most one new token (an error tone).
- No behavior changes: the same sections, values, validation, save/discard, and capture semantics.

**Non-Goals:**
- Removing Startup/Appearance sections (mockup is a style reference, not a content list — confirmed with the user).
- New preferences, new windows, or theming other windows.

## Decisions

### 1. Window-scoped `Themes/SettingsStyles.xaml`

Follows the `WhatsNewStyles.xaml` pattern: merges `LaunchTheme`, `Sizing`, and `DarkTooltips`, and holds keyed styles — section label, hint, dark input (`TextBox`), checkbox, footer strip, accent/neutral buttons, segmented control, hotkey chips. No implicit styles leak into other windows; the palette stays global-only in `App.xaml`.

### 2. Drawn chrome copied from the What's New pattern

`WindowStyle="None"`, drawn header (title + close button), `OnSourceInitialized` → `DwmApi.TryRoundCorners`, header drag, Escape closes. The window keeps `CanResize`, its current size, its single-instance launcher, and its ownership by the launch window (stays visible above it).

### 3. Segmented controls as a styled `ListBox`

One horizontal `ListBox` (or `ItemsControl` with selection) per selector, template: bordered group, chip-shaped segments, selected segment filled with `AccentSelectionBrush`/accent text. Bound to the existing `SelectedRuntime` / `SelectedPathDisplay`; the VM's long option labels ("cmd — Command Prompt") stay in the VM but the segments render a new short-token property (`cmd`, `ps`, `wt`), keeping `RuntimeKinds.ToToken` semantics untouched.

Alternative considered: restyled `RadioButton`s — same result, more template work per item; the `ListBox` gives selection and keyboard focus for one template.

### 4. Hotkey chips from a computed property

The VM exposes `HotkeyParts` (read-only collection of strings) derived from `HotkeyText` by splitting on `+`; the field renders an `ItemsControl` of chip borders plus a caret glyph when focused. Capture stays in the existing code-behind (`PreviewKeyDown`); `SetCapturedHotkey`/`SetHotkeyError` keep working — only presentation changes. Chip splitting is a pure function, so it gets a small unit test if extracted into a formatter (the tests project references Core, so the formatter would live in Core, like `MiddleEllipsisFormatter`).

### 5. One new token: `DangerBrush`

Validation errors currently use light-theme red `#C0392B`, which is unreadable on dark. The mockup has no error state, and reusing accent or muted brushes would blur its meaning. Add exactly one brush, a muted red consistent with the palette's saturation; everything else reuses existing brushes.

### 6. Update status line

`UpdateMessage` keeps its VM-driven text; the `TextBlock` gains the accent-toned status style. No VM changes.

### 7. Scrollbar theme at the application level

The slim dark scrollbar that the launch window's lists were meant to use lived as an implicit `ScrollBar` style in window resources — which never actually applied, because WPF does not match window-scoped implicit styles against the scrollbars inside `ScrollViewer`/`ListBox` templates. The style moved to a shared `Themes/DarkScrollBar.xaml` merged in `App.xaml`; application-level implicit styles do reach template parts, so the launch window, Settings, and What's New all get the themed scrollbar. This is the one deliberate exception to the window-scoped-styles convention, documented in `App.xaml`.

## Risks / Trade-offs

- [Drawn chrome on a resizable window: resize grips and min-size behavior need checking] → Mirror `WhatsNewWindow` (already `CanResize` with drawn chrome); verify resize during the manual pass.
- [Segmented control must remain keyboard-accessible] → `ListBox` selection gives arrow-key navigation and focus visuals for free; verify Tab/arrow behavior manually.
- [Chip rendering for unusual hotkeys (named keys like Enter, F1–F24, Space)] → Chips are just the parsed token strings `HotkeyParser.Format` already produces; long tokens wrap or ellipsize within the field — verify with a named-key hotkey during the manual pass.

## Migration Plan

None — styling only; preferences and config keys unchanged.

## Open Questions

None.

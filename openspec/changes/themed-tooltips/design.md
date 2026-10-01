# Design

## Context

The dark visual language already exists: `DarkContextMenu`/`DarkMenuItem` in `LaunchWindowStyles.xaml` define the dark popover look (PopoverBgBrush background, FieldStrokeBrush border, `Sizing.CornerRadius.Chunky` corners, soft drop shadow). Tooltips today are unstyled WPF `ToolTip`s — the default light system popup. Eight tooltips exist: six in `LaunchWindow.xaml`, two in `WhatsNewWindow.xaml`. `App.xaml` deliberately merges only the palette ("no implicit styles, so other windows are unaffected"), and each window merges its own style dictionary.

## Goals / Non-Goals

**Goals:**
- One dark tooltip style, visually consistent with the dark popover language.
- Applied to every tooltip in the launch window and the What's New window.
- Shared through theme resources without breaking the window-scoped styles convention.

**Non-Goals:**
- Custom timing, placement, or open/close animations (platform defaults stay).
- The tray icon hover tooltip (shell-drawn, not reachable from WPF styling).
- Restyling the Settings window (light theme per its spec; it has no tooltips).

## Decisions

### 1. Keyed `DarkToolTip` style, not an implicit style

Follow the `DarkContextMenu` precedent: a keyed style referenced explicitly by tooltips. This keeps the "no implicit styles leak across windows" guarantee — the Settings window, if it ever gains tooltips, keeps the system style unless it opts in.

### 2. Shared dictionary `Themes/DarkTooltips.xaml`

`WhatsNewStyles.xaml` merges `LaunchTheme` + `Sizing` but not `LaunchWindowStyles`, and merging the whole launch dictionary into What's New would drag every launch style along. Instead the `DarkToolTip` style lives in a small new `Themes/DarkTooltips.xaml` (merging `LaunchTheme` + `Sizing` like the other dictionaries), merged by both `LaunchWindowStyles.xaml` and `WhatsNewStyles.xaml`. If more shared dark-control styles appear later, they have a home.

Alternative considered: duplicating the style into both window dictionaries — rejected, two copies of the same template drift apart.

### 3. Template mirrors `DarkContextMenu`

A `ControlTemplate` with a rounded `Border` (PopoverBgBrush, FieldStrokeBrush 1px, Chunky corner radius, drop shadow) hosting the `ContentPresenter`, text in TextPrimaryBrush at the window body font size. A tooltip is smaller than a menu, so the shadow is tighter and padding moderate; exact values land during implementation against the live window.

### 4. No code-behind changes

The style is pure XAML; the eight `ToolTip="…"` attributes stay as they are.

## Risks / Trade-offs

- [Drop shadow on a `ToolTip` popup can clip or look heavy on some DPI settings] → Keep the shadow subtle; verify at 100%/150% scaling during manual pass; drop the effect if it renders poorly (the popover look survives without it).
- [Tooltip popup is top-level and does not inherit the window's rounded-corner treatment] → Corner rounding comes from the template's own Border, so this is unaffected.

## Migration Plan

None — pure styling, no data or configuration.

## Open Questions

None.

## Context

The Settings window is 760x560 by default with a 220px sidebar and 24px page padding, so the theme
picker has roughly 460px of usable width. The existing `SegmentGroup` style is a horizontal, bordered
group intended for short labels. The target design instead uses three self-contained cards, each with
a palette preview above the name and description.

## Goals / Non-Goals

**Goals**

- Make each theme's appearance recognizable before it is saved.
- Keep the selected card explicit through an accent ring and a check badge, not only through a
  background tint.
- Reuse the existing settings palette, corner-radius, spacing, and typography tokens.
- Keep the picker usable from the window's minimum width (640px) without horizontal scrolling.

**Non-Goals**

- No live theme switching; the theme still applies on the next start, as the hint says.
- No new theme values, tokens, or configuration keys.
- No change to the accent color selector or path display selector.

## Decisions

- **A `ListBox` with a `UniformGrid` panel** (`Rows=1`) keeps the existing selection model and
  two-way `SelectedItem` binding while giving all three cards equal widths. The items carry an 8px
  trailing margin and the list cancels it with a negative right margin, so the outer edges stay
  flush with the setting card's padding.
- **The card chrome lives in the `ListBoxItem` template** (`ThemeOptionItem`): a bordered surface, a
  hover border, and a 2px accent ring drawn as an overlay (`Margin="-1"`, `IsHitTestVisible=False`)
  so selection never shifts the layout.
- **The preview is fixed-color data, not theme resources**: the dark, light, and split previews must
  look the same regardless of the active application theme. The dot reuses `AccentBrush` for the
  dark and light cards; the system card uses a neutral gray dot, matching the split sample.
- **The check badge visibility is driven by the ancestor `ListBoxItem.IsSelected`** with a
  `DataTrigger`, so the view model carries no WPF types or selection state.
- **`AppThemeOption` gains a `Description`** rather than hard-coding per-theme copy in XAML, keeping
  labels and descriptions in the same place as the other `*Option` records.

## Risks / Trade-offs

- At the minimum window width each card is about 115px wide, so descriptions wrap to several lines.
  The cards share a uniform row height, and text wraps instead of clipping, so nothing is lost.
- The preview cards always render all three palettes, which is slightly more color in the Appearance
  page; that is the point of a preview, and the colors are confined to the card previews.
